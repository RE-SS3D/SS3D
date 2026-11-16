#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace SS3D.Editor.Perf
{
    /// <summary>
    /// Exports a ranked, agent-friendly summary of the Editor Profiler capture to
    /// <c>Logs/perf/*.md</c>. Analysis lives in <c>.cursor/skills/analyze-unity-perf</c>.
    /// </summary>
    public static class ProfilerCaptureExporter
    {
        private const int TopN = 30;
        private const int DefaultLastFrameCount = 300;
        private const string RelativeOutputDir = "Logs/perf";
        private const int MainThreadIndex = 0;

        /// <summary>Sample-name prefixes / substrings treated as game / marker hits in the report.</summary>
        private static readonly string[] GameMarkerPrefixes =
        {
            "SS3D",
            "Vision.",
            "Atmos",
            "FishNet",
        };

        [MenuItem("SS3D/Perf/Export Current Profiler Capture…")]
        public static void ExportCurrentCaptureMenu()
        {
            TryExportAndNotify(maxFrames: null);
        }

        [MenuItem("SS3D/Perf/Export Last 300 Frames…")]
        public static void ExportLast300FramesMenu()
        {
            TryExportAndNotify(maxFrames: DefaultLastFrameCount);
        }

        /// <summary>
        /// Aggregates hierarchy samples over the loaded Profiler frame range (optionally clamped)
        /// and writes the markdown report after a save-file prompt. Returns false when there is
        /// no capture data or the user cancels.
        /// </summary>
        public static bool TryExport(int? maxFrames, out string outputPath, out string error)
        {
            outputPath = null;
            error = null;

            if (!TryResolveFrameRange(maxFrames, out int firstFrame, out int lastFrame, out error))
            {
                return false;
            }

            string defaultDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativeOutputDir));
            Directory.CreateDirectory(defaultDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string defaultName = $"capture-{timestamp}.md";
            string chosenPath = EditorUtility.SaveFilePanel(
                "Export Profiler Capture",
                defaultDir,
                defaultName,
                "md");

            if (string.IsNullOrEmpty(chosenPath))
            {
                error = "Export cancelled.";
                return false;
            }

            return TryExportToPath(chosenPath, firstFrame, lastFrame, out outputPath, out error);
        }

        /// <summary>
        /// Batchmode / automation entry: validates the empty-capture path, writes a contract
        /// fixture under <c>Logs/perf/</c>, and exports live frames when the Profiler buffer
        /// already has data (no save dialog).
        /// </summary>
        public static void VerifyBatchMode()
        {
            string perfDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativeOutputDir));
            Directory.CreateDirectory(perfDir);

            string fixturePath = Path.Combine(perfDir, "_contract-fixture.md");
            File.WriteAllText(fixturePath, BuildContractFixtureMarkdown(), new UTF8Encoding(false));
            Debug.Log($"[SS3D.Perf] Wrote contract fixture to {fixturePath}");

            if (!TryResolveFrameRange(DefaultLastFrameCount, out int firstFrame, out int lastFrame, out string noFramesError))
            {
                Debug.Log($"[SS3D.Perf] No live Profiler frames (expected in headless verify): {noFramesError}");
                if (!File.Exists(fixturePath) || new FileInfo(fixturePath).Length > 64 * 1024)
                {
                    throw new InvalidOperationException("Contract fixture missing or unexpectedly large.");
                }

                AssertFixtureShape(File.ReadAllText(fixturePath));
                Debug.Log("[SS3D.Perf] VerifyBatchMode OK (fixture only).");
                return;
            }

            string livePath = Path.Combine(perfDir, "_batch-live-export.md");
            if (!TryExportToPath(livePath, firstFrame, lastFrame, out string written, out string exportError))
            {
                throw new InvalidOperationException($"Live export failed: {exportError}");
            }

            string body = File.ReadAllText(written);
            AssertFixtureShape(body);
            long size = new FileInfo(written).Length;
            Debug.Log($"[SS3D.Perf] VerifyBatchMode OK (live export {size} bytes at {written}).");
            if (size > 512 * 1024)
            {
                throw new InvalidOperationException($"Live export too large for agent use: {size} bytes.");
            }
        }

        private static bool TryExportToPath(
            string path,
            int firstFrame,
            int lastFrame,
            out string outputPath,
            out string error)
        {
            outputPath = null;
            error = null;

            string scenario = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(scenario))
            {
                scenario = "capture";
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RelativeOutputDir);

            CaptureAggregate aggregate = AggregateFrames(firstFrame, lastFrame);
            string markdown = FormatReport(scenario, firstFrame, lastFrame, aggregate);
            File.WriteAllText(path, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            outputPath = path;
            return true;
        }

        private static bool TryResolveFrameRange(
            int? maxFrames,
            out int firstFrame,
            out int lastFrame,
            out string error)
        {
            firstFrame = ProfilerDriver.firstFrameIndex;
            lastFrame = ProfilerDriver.lastFrameIndex;
            error = null;

            if (firstFrame < 0 || lastFrame < firstFrame)
            {
                error =
                    "No Profiler frames loaded. Open the Profiler window, record Play Mode (or load a capture), then retry.";
                return false;
            }

            if (maxFrames is > 0)
            {
                int span = lastFrame - firstFrame + 1;
                if (span > maxFrames.Value)
                {
                    firstFrame = lastFrame - maxFrames.Value + 1;
                }
            }

            return true;
        }

        private static string BuildContractFixtureMarkdown()
        {
            var aggregate = new CaptureAggregate
            {
                ByName = new Dictionary<string, SampleStats>(StringComparer.Ordinal)
                {
                    ["Vision.VisionMap"] = new SampleStats
                    {
                        SelfMs = 12.5,
                        TotalMs = 14.0,
                        GcBytes = 0,
                        Calls = 120,
                    },
                    ["GC.Alloc"] = new SampleStats
                    {
                        SelfMs = 0.2,
                        TotalMs = 0.2,
                        GcBytes = 48000,
                        Calls = 60,
                    },
                    ["WaitForTargetFPS"] = new SampleStats
                    {
                        SelfMs = 8.0,
                        TotalMs = 8.0,
                        GcBytes = 0,
                        Calls = 120,
                    },
                },
                FrameTimeSumMs = 2000,
                FramesRead = 120,
                FramesSkipped = 0,
                DeepProfiling = false,
            };

            return FormatReport("contract-fixture", 0, 119, aggregate);
        }

        private static void AssertFixtureShape(string markdown)
        {
            string[] required =
            {
                "# Perf capture",
                "- scenario:",
                "- avg_frame_ms:",
                "- deep_profile:",
                "## Top self-time",
                "## Top GC Alloc",
                "## SS3D / marker hits",
                "| rank | name | self_ms | self_% | calls | gc_bytes |",
            };

            for (int i = 0; i < required.Length; i++)
            {
                if (markdown.IndexOf(required[i], StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException($"Report missing required section/header: {required[i]}");
                }
            }
        }

        private static void TryExportAndNotify(int? maxFrames)
        {
            if (!TryExport(maxFrames, out string path, out string error))
            {
                if (!string.IsNullOrEmpty(error) && error != "Export cancelled.")
                {
                    EditorUtility.DisplayDialog("SS3D Perf Export", error, "OK");
                    Debug.LogWarning($"[SS3D.Perf] {error}");
                }

                return;
            }

            Debug.Log($"[SS3D.Perf] Wrote profiler summary to {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static CaptureAggregate AggregateFrames(int firstFrame, int lastFrame)
        {
            var byName = new Dictionary<string, SampleStats>(StringComparer.Ordinal);
            double frameTimeSumMs = 0;
            int framesRead = 0;
            int framesSkipped = 0;
            int totalFrames = lastFrame - firstFrame + 1;

            try
            {
                for (int frame = firstFrame; frame <= lastFrame; frame++)
                {
                    float progress = (float)(frame - firstFrame) / Math.Max(1, totalFrames);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "SS3D Perf Export",
                            $"Reading frame {frame} ({frame - firstFrame + 1}/{totalFrames})",
                            progress))
                    {
                        break;
                    }

                    HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                        frame,
                        MainThreadIndex,
                        HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                        HierarchyFrameDataView.columnSelfTime,
                        false);

                    if (!view.valid)
                    {
                        framesSkipped++;
                        view.Dispose();
                        continue;
                    }

                    frameTimeSumMs += view.frameTimeMs;
                    framesRead++;

                    AccumulateFrame(view, byName);
                    view.Dispose();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return new CaptureAggregate
            {
                ByName = byName,
                FrameTimeSumMs = frameTimeSumMs,
                FramesRead = framesRead,
                FramesSkipped = framesSkipped,
                DeepProfiling = ProfilerDriver.deepProfiling,
            };
        }

        private static void AccumulateFrame(HierarchyFrameDataView view, Dictionary<string, SampleStats> byName)
        {
            var stack = new Stack<int>();
            var children = new List<int>(64);
            int rootId = view.GetRootItemID();
            children.Clear();
            view.GetItemChildren(rootId, children);
            for (int i = 0; i < children.Count; i++)
            {
                stack.Push(children[i]);
            }

            while (stack.Count > 0)
            {
                int itemId = stack.Pop();
                if (itemId == HierarchyFrameDataView.invalidSampleId)
                {
                    continue;
                }

                string name = view.GetItemName(itemId);
                if (!string.IsNullOrEmpty(name))
                {
                    float selfMs = view.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnSelfTime);
                    float totalMs = view.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnTotalTime);
                    float gcBytes = view.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnGcMemory);
                    float calls = view.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnCalls);

                    if (!byName.TryGetValue(name, out SampleStats stats))
                    {
                        stats = new SampleStats();
                        byName[name] = stats;
                    }

                    stats.SelfMs += selfMs;
                    stats.TotalMs += totalMs;
                    stats.GcBytes += gcBytes;
                    stats.Calls += calls;
                }

                children.Clear();
                view.GetItemChildren(itemId, children);
                for (int i = 0; i < children.Count; i++)
                {
                    stack.Push(children[i]);
                }
            }
        }

        private static string FormatReport(string scenario, int firstFrame, int lastFrame, CaptureAggregate aggregate)
        {
            var sb = new StringBuilder(8192);
            double avgFrameMs = aggregate.FramesRead > 0
                ? aggregate.FrameTimeSumMs / aggregate.FramesRead
                : 0;

            sb.AppendLine("# Perf capture");
            sb.AppendLine($"- scenario: {scenario}");
            sb.AppendLine("- source: Editor Profiler / main thread hierarchy");
            sb.AppendLine($"- frames: {firstFrame}–{lastFrame} (read {aggregate.FramesRead}, skipped {aggregate.FramesSkipped})");
            sb.AppendLine($"- avg_frame_ms: {avgFrameMs.ToString("0.###", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"- deep_profile: {(aggregate.DeepProfiling ? "true" : "false")}");
            sb.AppendLine();

            List<KeyValuePair<string, SampleStats>> selfRanked = aggregate.ByName
                .OrderByDescending(kv => kv.Value.SelfMs)
                .Take(TopN)
                .ToList();

            List<KeyValuePair<string, SampleStats>> gcRanked = aggregate.ByName
                .Where(kv => kv.Value.GcBytes > 0)
                .OrderByDescending(kv => kv.Value.GcBytes)
                .Take(TopN)
                .ToList();

            AppendTable(sb, "## Top self-time", selfRanked, aggregate.FrameTimeSumMs, includeSelfPercent: true);
            sb.AppendLine();
            AppendTable(sb, "## Top GC Alloc", gcRanked, aggregate.FrameTimeSumMs, includeSelfPercent: false);
            sb.AppendLine();

            List<KeyValuePair<string, SampleStats>> markerHits = aggregate.ByName
                .Where(kv => IsGameMarker(kv.Key))
                .OrderByDescending(kv => kv.Value.SelfMs)
                .Take(TopN)
                .ToList();

            sb.AppendLine("## SS3D / marker hits");
            if (markerHits.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                AppendTableRows(sb, markerHits, aggregate.FrameTimeSumMs, includeSelfPercent: true);
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static void AppendTable(
            StringBuilder sb,
            string heading,
            List<KeyValuePair<string, SampleStats>> rows,
            double frameTimeSumMs,
            bool includeSelfPercent)
        {
            sb.AppendLine(heading);
            if (rows.Count == 0)
            {
                sb.AppendLine("(none)");
                return;
            }

            AppendTableRows(sb, rows, frameTimeSumMs, includeSelfPercent);
        }

        private static void AppendTableRows(
            StringBuilder sb,
            List<KeyValuePair<string, SampleStats>> rows,
            double frameTimeSumMs,
            bool includeSelfPercent)
        {
            sb.AppendLine("| rank | name | self_ms | self_% | calls | gc_bytes |");
            sb.AppendLine("| --- | --- | ---: | ---: | ---: | ---: |");

            for (int i = 0; i < rows.Count; i++)
            {
                string name = rows[i].Key.Replace("|", "\\|", StringComparison.Ordinal);
                SampleStats s = rows[i].Value;
                double selfPct = frameTimeSumMs > 0 ? 100.0 * s.SelfMs / frameTimeSumMs : 0;
                string selfPctText = includeSelfPercent
                    ? selfPct.ToString("0.##", CultureInfo.InvariantCulture)
                    : "—";

                sb.Append("| ");
                sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                sb.Append(" | ");
                sb.Append(name);
                sb.Append(" | ");
                sb.Append(s.SelfMs.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(" | ");
                sb.Append(selfPctText);
                sb.Append(" | ");
                sb.Append(s.Calls.ToString("0.#", CultureInfo.InvariantCulture));
                sb.Append(" | ");
                sb.Append(s.GcBytes.ToString("0.#", CultureInfo.InvariantCulture));
                sb.AppendLine(" |");
            }
        }

        private static bool IsGameMarker(string name)
        {
            for (int i = 0; i < GameMarkerPrefixes.Length; i++)
            {
                string prefix = GameMarkerPrefixes[i];
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // FishNet often appears mid-path rather than as a root sample name.
                if (prefix.Equals("FishNet", StringComparison.Ordinal)
                    && name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class SampleStats
        {
            public double SelfMs;
            public double TotalMs;
            public double GcBytes;
            public double Calls;
        }

        private sealed class CaptureAggregate
        {
            public Dictionary<string, SampleStats> ByName;
            public double FrameTimeSumMs;
            public int FramesRead;
            public int FramesSkipped;
            public bool DeepProfiling;
        }
    }
}
#endif
