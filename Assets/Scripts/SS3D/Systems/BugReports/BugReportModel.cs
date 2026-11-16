using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SS3D.Systems.BugReports
{
    /// <summary>Severity buttons in the dialog; <see cref="None"/> leaves triage to a developer.</summary>
    public enum BugReportSeverity
    {
        None,
        Minor,
        Major,
        Blocker,
    }

    /// <summary>
    /// The moment a report describes, frozen when the dialog opened (<c>sandbox-server.md</c> §7): what the
    /// client saw, never what it sees when the player presses Send. Sent with the report and listed in
    /// the dialog's "Logged automatically" section.
    /// </summary>
    [Serializable]
    public sealed class BugReportClientInfo
    {
        public string version;
        /// <summary>0 when no round has started on this server yet.</summary>
        public int roundId;
        public int roundSeconds;
        /// <summary>Lobby, warming up, ongoing, ending.</summary>
        public string roundState;
        public string map;
        public bool unsavedEdits;
        /// <summary>Badge text, e.g. "IN ROUND · ALIVE", "GHOST", "LOBBY", "MAP EDITOR · AREA TOOL".</summary>
        public string context;
        public bool hasPosition;
        public Vector3 position;
        /// <summary>Area name at <see cref="position"/>, or empty when unknown.</summary>
        public string area;
        /// <summary>"Own host", "Host", "Client".</summary>
        public string networkRole;
        public int players;
        public int fps;
        public string os;
        public string gpu;
        public string playerName;
        /// <summary>UTC time the dialog opened.</summary>
        public string capturedAtUtc;
    }

    /// <summary>Pure helpers shared by the runtime dialog, the server and the editor preview.</summary>
    public static class BugReportText
    {
        public const int MaxSummaryLength = 80;
        public const int MaxDetailsLength = 4000;
        public const int MaxLogLines = 60;
        public const int MaxLogLineLength = 500;
        public const int MaxScreenshotBytes = 512 * 1024;

        public static string SeverityKey(BugReportSeverity severity) => severity switch
        {
            BugReportSeverity.Minor => "minor",
            BugReportSeverity.Major => "major",
            BugReportSeverity.Blocker => "blocker",
            _ => "",
        };

        /// <summary>Unknown or blank text becomes <see cref="BugReportSeverity.None"/>.</summary>
        public static BugReportSeverity ParseSeverity(string key) => key switch
        {
            "minor" => BugReportSeverity.Minor,
            "major" => BugReportSeverity.Major,
            "blocker" => BugReportSeverity.Blocker,
            _ => BugReportSeverity.None,
        };

        /// <summary>"00:14:32".</summary>
        public static string FormatClock(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
        }

        /// <summary>"0:42", for the rate-limit notice.</summary>
        public static string FormatCountdown(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{whole / 60}:{whole % 60:00}";
        }

        /// <summary>The line under the window title: "ROUND #1187 · CAPTURED AT 00:14:32", or the lobby variant.</summary>
        public static string CaptureLine(BugReportClientInfo info)
        {
            if (info.roundId > 0 && info.roundState != "Lobby")
                return $"ROUND #{info.roundId} · CAPTURED AT {FormatClock(info.roundSeconds)}";

            string clock = DateTime.TryParse(info.capturedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime utc)
                ? utc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                : "";
            return $"LOBBY · CAPTURED AT {clock}";
        }

        /// <summary>Trims and clamps the player's text the same way on client and server.</summary>
        public static string Clamp(string text, int max)
        {
            text = (text ?? "").Trim();
            return text.Length > max ? text.Substring(0, max) : text;
        }

        /// <summary>Clamps line count and length so a log storm can't blow up a report.</summary>
        public static string[] ClampLog(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return Array.Empty<string>();

            int start = Mathf.Max(0, lines.Count - MaxLogLines);
            string[] result = new string[lines.Count - start];
            for (int i = start; i < lines.Count; i++)
                result[i - start] = Clamp(lines[i], MaxLogLineLength);
            return result;
        }

        /// <summary>
        /// The "Logged automatically" rows, in the order the design lists them. <paramref name="local"/>
        /// replaces the log tail row, because a local bundle carries the whole log files.
        /// </summary>
        public static List<KeyValuePair<string, string>> MetadataRows(BugReportClientInfo info, int logLines, bool local)
        {
            string round = info.roundId > 0 ? $"#{info.roundId} · {FormatClock(info.roundSeconds)}" : "No round yet";
            string map = string.IsNullOrEmpty(info.map) ? "None loaded" : $"{info.map} · unsaved edits: {(info.unsavedEdits ? "yes" : "no")}";
            string position = info.hasPosition
                ? string.Format(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0}, {2:0.0})", info.position.x, info.position.y, info.position.z)
                : "—";
            if (info.hasPosition && !string.IsNullOrEmpty(info.area))
                position = info.area + " " + position;

            return new List<KeyValuePair<string, string>>
            {
                new("Game version", info.version),
                new("Round", round),
                new("Map", map),
                new("Context", SentenceCase(info.context)),
                new("Position", position),
                new("Network role", info.networkRole),
                new("Players", info.players.ToString(CultureInfo.InvariantCulture)),
                new("FPS", info.fps.ToString(CultureInfo.InvariantCulture)),
                new("OS / GPU", $"{info.os} · {info.gpu}"),
                new("Player name", info.playerName),
                local ? new("Log files", "Included in the bundle") : new("Recent log", $"{logLines} lines"),
            };
        }

        /// <summary>"IN ROUND · ALIVE" → "In round · alive".</summary>
        public static string SentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string lower = text.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }
    }
}
