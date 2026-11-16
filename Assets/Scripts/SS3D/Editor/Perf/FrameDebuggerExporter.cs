#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Perf
{
    /// <summary>
    /// Exports a ranked, agent-friendly summary of the Editor Frame Debugger capture to
    /// <c>Logs/framedebug/*.md</c>. Analysis lives in <c>.cursor/skills/analyze-unity-framedebug</c>.
    /// </summary>
    public static class FrameDebuggerExporter
    {
        private const int TopN = 30;
        private const int MaxFullDrawEvents = 800;
        private const int FullWaitFrames = 2;
        private const int MaxReportBytes = 512 * 1024;
        private const string RelativeOutputDir = "Logs/framedebug";

        private static readonly string[] FeatureTokens =
        {
            "Selection Pick",
            "Atmos",
            "Vision",
            "UiBackdrop",
            "SS3D",
            "STDefault",
            "Simple Toon",
            "ObjectIcon",
        };

        private static readonly FrameDebuggerReflection Api = new FrameDebuggerReflection();

        private static bool _fullExportRunning;
        private static FullExportState _fullState;

        [MenuItem("SS3D/Perf/Export Frame Debugger (Quick)…")]
        public static void ExportQuickMenu()
        {
            TryExportAndNotify(fullDetail: false);
        }

        [MenuItem("SS3D/Perf/Export Frame Debugger (Full)…")]
        public static void ExportFullMenu()
        {
            TryExportAndNotify(fullDetail: true);
        }

        /// <summary>
        /// Batchmode / automation entry: writes a contract fixture under <c>Logs/framedebug/</c>.
        /// Does not require a live Frame Debugger capture (headless has none).
        /// </summary>
        public static void VerifyBatchMode()
        {
            string dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativeOutputDir));
            Directory.CreateDirectory(dir);

            string fixturePath = Path.Combine(dir, "_contract-fixture.md");
            File.WriteAllText(fixturePath, BuildContractFixtureMarkdown(), new UTF8Encoding(false));
            AssertFixtureShape(File.ReadAllText(fixturePath));

            long size = new FileInfo(fixturePath).Length;
            if (size > 64 * 1024)
            {
                throw new InvalidOperationException($"Contract fixture unexpectedly large: {size} bytes.");
            }

            Debug.Log($"[SS3D.FrameDebug] VerifyBatchMode OK (fixture {size} bytes at {fixturePath}).");
        }

        /// <summary>
        /// Synchronous Quick export, or kicks off async Full export. Returns false when cancelled
        /// or when Quick fails immediately. Full success is reported via dialog when finished.
        /// </summary>
        public static bool TryExport(bool fullDetail, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            if (_fullExportRunning)
            {
                error = "A Full Frame Debugger export is already running.";
                return false;
            }

            if (!TryValidateCapture(out error))
            {
                return false;
            }

            string defaultDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativeOutputDir));
            Directory.CreateDirectory(defaultDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string modeTag = fullDetail ? "full" : "quick";
            string defaultName = $"framedebug-{modeTag}-{timestamp}.md";
            string chosenPath = EditorUtility.SaveFilePanel(
                fullDetail ? "Export Frame Debugger (Full)" : "Export Frame Debugger (Quick)",
                defaultDir,
                defaultName,
                "md");

            if (string.IsNullOrEmpty(chosenPath))
            {
                error = "Export cancelled.";
                return false;
            }

            if (!fullDetail)
            {
                return TryExportQuickToPath(chosenPath, out outputPath, out error);
            }

            return TryStartFullExport(chosenPath, out outputPath, out error);
        }

        private static void TryExportAndNotify(bool fullDetail)
        {
            if (!TryExport(fullDetail, out string path, out string error))
            {
                if (!string.IsNullOrEmpty(error) && error != "Export cancelled.")
                {
                    EditorUtility.DisplayDialog("SS3D Frame Debugger Export", error, "OK");
                    Debug.LogWarning($"[SS3D.FrameDebug] {error}");
                }

                return;
            }

            if (fullDetail)
            {
                Debug.Log($"[SS3D.FrameDebug] Full export started → {path}");
                return;
            }

            Debug.Log($"[SS3D.FrameDebug] Wrote Frame Debugger summary to {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static bool TryValidateCapture(out string error)
        {
            error = null;
            Api.EnsureDiscovered();
            if (!Api.IsReady)
            {
                error = Api.DiscoverError
                    ?? "Frame Debugger reflection binding failed for this Unity version.";
                return false;
            }

            if (!UnityEngine.FrameDebugger.enabled)
            {
                error =
                    "Frame Debugger is not enabled. Open Window > Analysis > Frame Debugger, click Enable "
                    + "(Play Mode will pause), then retry. On Linux prefer Vulkan over OpenGL.";
                return false;
            }

            int count = Api.GetCount();
            if (count <= 0)
            {
                error =
                    "Frame Debugger has 0 events. Enable it while Play Mode is rendering a Game view, "
                    + "wait for the event tree to populate, then retry. Empty trees are common on OpenGL.";
                return false;
            }

            return true;
        }

        private static bool TryExportQuickToPath(string path, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            CaptureAggregate aggregate = AggregateQuick();
            if (aggregate.EventCount == 0)
            {
                error = "No Frame Debugger events could be read.";
                return false;
            }

            string scenario = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(scenario))
            {
                scenario = "framedebug";
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RelativeOutputDir);
            string markdown = FormatReport(scenario, "quick", aggregate);
            File.WriteAllText(path, markdown, new UTF8Encoding(false));

            long size = new FileInfo(path).Length;
            if (size > MaxReportBytes)
            {
                error = $"Export too large for agent use: {size} bytes (max {MaxReportBytes}).";
                return false;
            }

            outputPath = path;
            return true;
        }

        private static bool TryStartFullExport(string path, out string outputPath, out string error)
        {
            outputPath = path;
            error = null;

            CaptureAggregate seed = AggregateQuick();
            if (seed.EventCount == 0)
            {
                error = "No Frame Debugger events could be read.";
                return false;
            }

            Array events = Api.GetFrameEvents();
            var drawIndices = new List<int>(Math.Min(seed.DrawEventCount, MaxFullDrawEvents));
            if (events != null)
            {
                int n = Math.Min(events.Length, MaxFullDrawEvents * 4);
                for (int i = 0; i < n && drawIndices.Count < MaxFullDrawEvents; i++)
                {
                    object ev = events.GetValue(i);
                    if (!Api.TryGetEventTypeName(ev, out string typeName))
                    {
                        continue;
                    }

                    if (FrameDebuggerReflection.IsDrawEventType(typeName))
                    {
                        drawIndices.Add(i);
                    }
                }
            }

            if (drawIndices.Count == 0)
            {
                // Fall back: treat every index as a candidate (still capped).
                int fallback = Math.Min(seed.EventCount, MaxFullDrawEvents);
                for (int i = 0; i < fallback; i++)
                {
                    drawIndices.Add(i);
                }
            }

            _fullState = new FullExportState
            {
                Path = path,
                Scenario = Path.GetFileNameWithoutExtension(path) ?? "framedebug",
                Aggregate = seed,
                DrawIndices = drawIndices,
                OriginalLimit = Api.GetLimit(),
                Index = 0,
                Phase = 0,
                WaitLeft = 0,
            };

            _fullExportRunning = true;
            EditorApplication.update += FullExportTick;
            EditorUtility.DisplayProgressBar(
                "SS3D Frame Debugger Full Export",
                $"Preparing {drawIndices.Count} draw events…",
                0f);
            return true;
        }

        private static void FullExportTick()
        {
            if (!_fullExportRunning || _fullState == null)
            {
                EditorApplication.update -= FullExportTick;
                return;
            }

            FullExportState state = _fullState;
            try
            {
                if (state.Index >= state.DrawIndices.Count)
                {
                    FinishFullExport(success: true, error: null);
                    return;
                }

                int eventIndex = state.DrawIndices[state.Index];
                float progress = (float)state.Index / Math.Max(1, state.DrawIndices.Count);

                if (state.Phase == 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "SS3D Frame Debugger Full Export",
                            $"Event {eventIndex} ({state.Index + 1}/{state.DrawIndices.Count})",
                            progress))
                    {
                        FinishFullExport(success: false, error: "Export cancelled.");
                        return;
                    }

                    // Frame Debugger limit is 1-based (select event N).
                    if (!Api.TrySetLimit(eventIndex + 1))
                    {
                        state.Index++;
                        state.Phase = 0;
                        return;
                    }

                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    EditorApplication.QueuePlayerLoopUpdate();
                    state.Phase = 1;
                    state.WaitLeft = FullWaitFrames;
                    return;
                }

                if (state.Phase == 1)
                {
                    if (state.WaitLeft > 0)
                    {
                        state.WaitLeft--;
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }

                    object data = Api.GetFrameEventData(eventIndex);
                    if (data != null)
                    {
                        AccumulateDetail(state.Aggregate, data);
                        state.DetailHits++;
                    }

                    state.Index++;
                    state.Phase = 0;
                }
            }
            catch (Exception e)
            {
                FinishFullExport(success: false, error: e.GetBaseException().Message);
            }
        }

        private static void FinishFullExport(bool success, string error)
        {
            EditorApplication.update -= FullExportTick;
            EditorUtility.ClearProgressBar();

            FullExportState state = _fullState;
            _fullState = null;
            _fullExportRunning = false;

            if (state == null)
            {
                return;
            }

            try
            {
                if (state.OriginalLimit > 0)
                {
                    Api.TrySetLimit(state.OriginalLimit);
                }
            }
            catch
            {
                // ignore restore failures
            }

            if (!success)
            {
                if (!string.IsNullOrEmpty(error) && error != "Export cancelled.")
                {
                    EditorUtility.DisplayDialog("SS3D Frame Debugger Export", error, "OK");
                    Debug.LogWarning($"[SS3D.FrameDebug] {error}");
                }

                return;
            }

            state.Aggregate.Mode = "full";
            state.Aggregate.DetailHits = state.DetailHits;
            string markdown = FormatReport(state.Scenario, "full", state.Aggregate);
            Directory.CreateDirectory(Path.GetDirectoryName(state.Path) ?? RelativeOutputDir);
            File.WriteAllText(state.Path, markdown, new UTF8Encoding(false));

            long size = new FileInfo(state.Path).Length;
            if (size > MaxReportBytes)
            {
                EditorUtility.DisplayDialog(
                    "SS3D Frame Debugger Export",
                    $"Export wrote {size} bytes (over {MaxReportBytes} agent budget). Trim scenario or use Quick.",
                    "OK");
            }

            Debug.Log(
                $"[SS3D.FrameDebug] Wrote Full Frame Debugger summary to {state.Path} "
                + $"(detail hits {state.DetailHits}/{state.DrawIndices.Count}, {size} bytes)");
            EditorUtility.RevealInFinder(state.Path);
        }

        private static CaptureAggregate AggregateQuick()
        {
            var aggregate = new CaptureAggregate
            {
                Mode = "quick",
                GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                PassCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                ObjectCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                TypeCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                FeatureCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                BatchBreakCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                ShaderCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                ShaderInstances = new Dictionary<string, long>(StringComparer.Ordinal),
            };

            Array events = Api.GetFrameEvents();
            int count = Api.GetCount();
            aggregate.EventCount = count;

            int n = events?.Length ?? count;
            for (int i = 0; i < n; i++)
            {
                string infoName = Api.GetFrameEventInfoName(i) ?? string.Empty;
                string typeName = null;
                UnityEngine.Object obj = null;

                if (events != null && i < events.Length)
                {
                    object ev = events.GetValue(i);
                    Api.TryGetEventTypeName(ev, out typeName);
                    obj = Api.GetEventObjectField(ev);
                }

                if (obj == null)
                {
                    obj = Api.GetFrameEventObject(i);
                }

                if (string.IsNullOrEmpty(typeName))
                {
                    typeName = "Unknown";
                }

                Increment(aggregate.TypeCounts, typeName);

                bool isDraw = FrameDebuggerReflection.IsDrawEventType(typeName);
                if (isDraw)
                {
                    aggregate.DrawEventCount++;
                }

                string passKey = PassKeyFromInfoName(infoName);
                if (!string.IsNullOrEmpty(passKey))
                {
                    Increment(aggregate.PassCounts, passKey);
                }

                if (isDraw)
                {
                    string objName = obj != null ? obj.name : "(none)";
                    if (string.IsNullOrEmpty(objName))
                    {
                        objName = "(unnamed)";
                    }

                    Increment(aggregate.ObjectCounts, objName);
                }

                AccumulateFeatureHits(aggregate, infoName, obj != null ? obj.name : null);
            }

            return aggregate;
        }

        private static void AccumulateDetail(CaptureAggregate aggregate, object data)
        {
            string shader = Api.GetField(data, "m_RealShaderName", string.Empty);
            if (string.IsNullOrEmpty(shader))
            {
                shader = Api.GetField(data, "m_OriginalShaderName", string.Empty);
            }

            int instances = Api.GetField(data, "m_InstanceCount", 0);
            int draws = Api.GetField(data, "m_DrawCallCount", 0);
            if (draws <= 0)
            {
                draws = 1;
            }

            if (!string.IsNullOrEmpty(shader))
            {
                Increment(aggregate.ShaderCounts, shader, draws);
                if (!aggregate.ShaderInstances.TryGetValue(shader, out long sum))
                {
                    sum = 0;
                }

                aggregate.ShaderInstances[shader] = sum + Math.Max(0, instances);
            }

            string pass = Api.GetField(data, "m_PassName", string.Empty);
            if (!string.IsNullOrEmpty(pass))
            {
                Increment(aggregate.PassCounts, "pass:" + pass, draws);
            }

            int causeInt = Api.GetField(data, "m_BatchBreakCause", 0);
            string[] causes = Api.GetBatchBreakCauseStrings();
            if (causeInt > 0 && causes != null && causeInt < causes.Length)
            {
                string cause = causes[causeInt];
                if (!string.IsNullOrEmpty(cause))
                {
                    Increment(aggregate.BatchBreakCounts, cause);
                }
            }

            // Path/object feature hits already counted in Quick seed — only add shader tokens here.
            if (!string.IsNullOrEmpty(shader))
            {
                AccumulateFeatureHits(aggregate, shader, null);
            }
        }

        private static void AccumulateFeatureHits(CaptureAggregate aggregate, string path, string extra)
        {
            string haystack = path ?? string.Empty;
            if (!string.IsNullOrEmpty(extra))
            {
                haystack = haystack + " " + extra;
            }

            if (string.IsNullOrEmpty(haystack))
            {
                return;
            }

            for (int i = 0; i < FeatureTokens.Length; i++)
            {
                string token = FeatureTokens[i];
                if (haystack.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Increment(aggregate.FeatureCounts, token);
                }
            }
        }

        private static string PassKeyFromInfoName(string infoName)
        {
            if (string.IsNullOrEmpty(infoName))
            {
                return "(unnamed)";
            }

            // Prefer URP-ish parent path: A/B/C → A/B
            int lastSlash = infoName.LastIndexOf('/');
            if (lastSlash > 0)
            {
                int prev = infoName.LastIndexOf('/', lastSlash - 1);
                if (prev >= 0)
                {
                    return infoName.Substring(0, lastSlash);
                }

                return infoName.Substring(0, lastSlash);
            }

            return infoName;
        }

        private static string FormatReport(string scenario, string mode, CaptureAggregate aggregate)
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("# Frame Debugger capture");
            sb.AppendLine($"- scenario: {scenario}");
            sb.AppendLine("- source: Editor Frame Debugger");
            sb.AppendLine($"- graphics_api: {aggregate.GraphicsApi}");
            sb.AppendLine($"- event_count: {aggregate.EventCount.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"- draw_events: {aggregate.DrawEventCount.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"- mode: {mode}");
            if (string.Equals(mode, "full", StringComparison.Ordinal))
            {
                sb.AppendLine(
                    $"- detail_hits: {aggregate.DetailHits.ToString(CultureInfo.InvariantCulture)}");
            }

            sb.AppendLine();

            AppendRankedIntTable(sb, "## Pass distribution", aggregate.PassCounts, TopN);
            sb.AppendLine();
            AppendRankedIntTable(sb, "## Top draw objects", aggregate.ObjectCounts, TopN);
            sb.AppendLine();

            sb.AppendLine("## Batch breaks");
            if (!string.Equals(mode, "full", StringComparison.Ordinal))
            {
                sb.AppendLine("(skipped — quick mode)");
            }
            else if (aggregate.BatchBreakCounts.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                AppendRankedIntRows(sb, aggregate.BatchBreakCounts, TopN, "cause", "count");
            }

            sb.AppendLine();
            sb.AppendLine("## Shader / pass hits");
            if (!string.Equals(mode, "full", StringComparison.Ordinal))
            {
                AppendRankedIntRows(sb, aggregate.TypeCounts, TopN, "event_type", "count");
            }
            else if (aggregate.ShaderCounts.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                sb.AppendLine("| rank | shader | draws | instances |");
                sb.AppendLine("| --- | --- | ---: | ---: |");
                List<KeyValuePair<string, int>> ranked = aggregate.ShaderCounts
                    .OrderByDescending(kv => kv.Value)
                    .Take(TopN)
                    .ToList();
                for (int i = 0; i < ranked.Count; i++)
                {
                    string name = EscapeCell(ranked[i].Key);
                    long instances = aggregate.ShaderInstances.TryGetValue(ranked[i].Key, out long sum)
                        ? sum
                        : 0;
                    sb.Append("| ");
                    sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                    sb.Append(" | ");
                    sb.Append(name);
                    sb.Append(" | ");
                    sb.Append(ranked[i].Value.ToString(CultureInfo.InvariantCulture));
                    sb.Append(" | ");
                    sb.Append(instances.ToString(CultureInfo.InvariantCulture));
                    sb.AppendLine(" |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## SS3D / feature hits");
            if (aggregate.FeatureCounts.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                AppendRankedIntRows(sb, aggregate.FeatureCounts, TopN, "token", "hits");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendRankedIntTable(
            StringBuilder sb,
            string heading,
            Dictionary<string, int> counts,
            int topN)
        {
            sb.AppendLine(heading);
            if (counts == null || counts.Count == 0)
            {
                sb.AppendLine("(none)");
                return;
            }

            AppendRankedIntRows(sb, counts, topN, "name", "count");
        }

        private static void AppendRankedIntRows(
            StringBuilder sb,
            Dictionary<string, int> counts,
            int topN,
            string nameHeader,
            string countHeader)
        {
            sb.Append("| rank | ");
            sb.Append(nameHeader);
            sb.Append(" | ");
            sb.Append(countHeader);
            sb.AppendLine(" |");
            sb.AppendLine("| --- | --- | ---: |");

            List<KeyValuePair<string, int>> ranked = counts
                .OrderByDescending(kv => kv.Value)
                .Take(topN)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                sb.Append("| ");
                sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                sb.Append(" | ");
                sb.Append(EscapeCell(ranked[i].Key));
                sb.Append(" | ");
                sb.Append(ranked[i].Value.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(" |");
            }
        }

        private static string EscapeCell(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
        }

        private static void Increment(Dictionary<string, int> map, string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount == 0)
            {
                return;
            }

            if (!map.TryGetValue(key, out int n))
            {
                n = 0;
            }

            map[key] = n + amount;
        }

        private static string BuildContractFixtureMarkdown()
        {
            var aggregate = new CaptureAggregate
            {
                Mode = "quick",
                GraphicsApi = "Vulkan",
                EventCount = 120,
                DrawEventCount = 80,
                DetailHits = 0,
                PassCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["UniversalRenderer/DrawOpaqueObjects"] = 40,
                    ["SS3D Selection Pick"] = 12,
                },
                ObjectCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["TileGrey"] = 25,
                    ["Human"] = 2,
                },
                TypeCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["Mesh"] = 50,
                    ["SRPBatch"] = 20,
                    ["InstancedMesh"] = 10,
                },
                FeatureCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["Selection Pick"] = 12,
                    ["STDefault"] = 30,
                },
                BatchBreakCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                ShaderCounts = new Dictionary<string, int>(StringComparer.Ordinal),
                ShaderInstances = new Dictionary<string, long>(StringComparer.Ordinal),
            };

            return FormatReport("contract-fixture", "quick", aggregate);
        }

        private static void AssertFixtureShape(string markdown)
        {
            string[] required =
            {
                "# Frame Debugger capture",
                "- scenario:",
                "- graphics_api:",
                "- event_count:",
                "- draw_events:",
                "- mode:",
                "## Pass distribution",
                "## Top draw objects",
                "## Batch breaks",
                "## Shader / pass hits",
                "## SS3D / feature hits",
            };

            for (int i = 0; i < required.Length; i++)
            {
                if (markdown.IndexOf(required[i], StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException($"Report missing required section/header: {required[i]}");
                }
            }
        }

        private sealed class CaptureAggregate
        {
            public string Mode;
            public string GraphicsApi;
            public int EventCount;
            public int DrawEventCount;
            public int DetailHits;
            public Dictionary<string, int> PassCounts;
            public Dictionary<string, int> ObjectCounts;
            public Dictionary<string, int> TypeCounts;
            public Dictionary<string, int> FeatureCounts;
            public Dictionary<string, int> BatchBreakCounts;
            public Dictionary<string, int> ShaderCounts;
            public Dictionary<string, long> ShaderInstances;
        }

        private sealed class FullExportState
        {
            public string Path;
            public string Scenario;
            public CaptureAggregate Aggregate;
            public List<int> DrawIndices;
            public int OriginalLimit;
            public int Index;
            public int Phase;
            public int WaitLeft;
            public int DetailHits;
        }
    }
}
#endif
