using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.BugReports
{
    /// <summary>Everything that goes into <c>report.json</c> in a local bundle.</summary>
    [Serializable]
    public sealed class BugReportBundleReport
    {
        public string summary;
        public string details;
        public string severity;
        public bool crashed;
        public bool screenshotIncluded;
        public bool mapIncluded;
        public BugReportClientInfo client;
        public string[] logTail;
    }

    /// <summary>
    /// Writes the local bug report bundle (<c>sandbox-server.md</c> §7, "the same dialog in a local session"):
    /// one zip in <c>Data/Exports/</c> with the report, the player's logs, the screenshot and, if chosen, the
    /// current map. Nothing here touches the saved map file.
    /// </summary>
    public static class BugReportBundle
    {
        /// <summary>Large Editor.log files are cut to their last part; the start of a session is rarely the bug.</summary>
        public const int MaxLogBytes = 2 * 1024 * 1024;

        /// <summary>"bug-1187-001432" for round 1187 at 00:14:32, or "bug-lobby-213359" by local clock.</summary>
        public static string BaseName(BugReportClientInfo info, DateTime localNow)
        {
            if (info.roundId > 0 && info.roundState != "Lobby")
            {
                int s = Mathf.Max(0, info.roundSeconds);
                return $"bug-{info.roundId}-{s / 3600:00}{s / 60 % 60:00}{s % 60:00}";
            }

            return $"bug-lobby-{localNow:HHmmss}";
        }

        /// <param name="logFiles">Paths to include under <c>logs/</c>; missing files are skipped.</param>
        /// <param name="mapJson">The current station template, or null to leave it out.</param>
        public static bool TryWrite(string exportsFolder, string baseName, BugReportBundleReport report,
            byte[] screenshotPng, IEnumerable<string> logFiles, string mapName, string mapJson,
            out string zipPath, out string error)
        {
            zipPath = null;
            error = null;
            try
            {
                Directory.CreateDirectory(exportsFolder);
                zipPath = UniquePath(exportsFolder, baseName);
                string temp = zipPath + ".tmp";
                if (File.Exists(temp))
                    File.Delete(temp);

                using (FileStream stream = new(temp, FileMode.CreateNew))
                using (ZipArchive archive = new(stream, ZipArchiveMode.Create))
                {
                    WriteText(archive, "report.json", JsonUtility.ToJson(report, true));
                    if (screenshotPng is { Length: > 0 })
                        WriteBytes(archive, "screenshot.png", screenshotPng);
                    if (mapJson != null)
                        WriteText(archive, $"map/{SafeFileName(mapName)}.json", mapJson);
                    if (logFiles != null)
                    {
                        HashSet<string> added = new();
                        foreach (string path in logFiles)
                        {
                            if (string.IsNullOrEmpty(path) || !File.Exists(path) || !added.Add(Path.GetFullPath(path)))
                                continue;
                            WriteBytes(archive, "logs/" + Path.GetFileName(path), ReadTail(path, MaxLogBytes));
                        }
                    }
                }

                File.Move(temp, zipPath);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>The log files a local player has: Unity's player/editor log and this process's SS3D logs.</summary>
        public static List<string> DefaultLogFiles()
        {
            List<string> files = new();
            if (!string.IsNullOrEmpty(UnityEngine.Application.consoleLogPath))
                files.Add(UnityEngine.Application.consoleLogPath);

            string folder = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../Logs"));
            if (Directory.Exists(folder))
            {
                foreach (string path in Directory.GetFiles(folder, "Log*.*"))
                {
                    string extension = Path.GetExtension(path);
                    if (extension is ".json" or ".log")
                        files.Add(path);
                }
            }

            return files;
        }

        private static string UniquePath(string folder, string baseName)
        {
            string path = Path.Combine(folder, baseName + ".zip");
            for (int i = 2; File.Exists(path); i++)
                path = Path.Combine(folder, $"{baseName}-{i}.zip");
            return path;
        }

        /// <summary>Reads at most the last <paramref name="maxBytes"/>; shares the file so a live log can be read.</summary>
        private static byte[] ReadTail(string path, int maxBytes)
        {
            using FileStream input = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            long length = input.Length;
            int count = (int)Math.Min(length, maxBytes);
            input.Seek(length - count, SeekOrigin.Begin);
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = input.Read(buffer, read, count - read);
                if (n <= 0)
                    break;
                read += n;
            }

            return read == count ? buffer : buffer.AsSpan(0, read).ToArray();
        }

        private static void WriteText(ZipArchive archive, string name, string text) =>
            WriteBytes(archive, name, Encoding.UTF8.GetBytes(text ?? ""));

        private static void WriteBytes(ZipArchive archive, string name, byte[] bytes)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
            using Stream output = entry.Open();
            output.Write(bytes, 0, bytes.Length);
        }

        private static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "map";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }

    /// <summary>Builds a pre-filled issue link for the project's GitHub issue forms (<c>.github/ISSUE_TEMPLATE</c>).</summary>
    public static class BugReportIssueLink
    {
        public const string NewIssueUrl = ProjectRepository.Url + "/issues/new";

        /// <summary>Browsers and GitHub reject very long URLs; the bundle carries the full text.</summary>
        private const int MaxFieldLength = 1500;

        public static string Build(BugReportBundleReport report, string bundleFileName, bool mapEditor)
        {
            StringBuilder what = new(report.summary);
            if (!string.IsNullOrEmpty(report.details))
                what.Append("\n\n").Append(report.details);
            if (report.crashed)
                what.Append("\n\nThe game crashed or froze.");
            if (!string.IsNullOrEmpty(report.severity))
                what.Append("\n\nSeverity: ").Append(report.severity);

            BugReportClientInfo c = report.client;
            string extra = $"Attach `{bundleFileName}` (in the game's `Data/Exports/` folder) here.\n\n"
                + $"Context: {c.context} · Round {c.roundId} at {BugReportText.FormatClock(c.roundSeconds)} · Map {c.map}\n"
                + $"{c.os} · {c.gpu} · {c.fps} FPS";

            return NewIssueUrl
                + "?template=" + (mapEditor ? "MapBug.yml" : "Bug.yml")
                + "&title=" + Escape(report.summary)
                + "&version=" + Escape(c.version)
                + "&what=" + Escape(what.ToString())
                + "&extra=" + Escape(extra);
        }

        private static string Escape(string text)
        {
            text ??= "";
            if (text.Length > MaxFieldLength)
                text = text.Substring(0, MaxFieldLength) + "…";
            return Uri.EscapeDataString(text);
        }
    }
}
