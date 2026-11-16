using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SS3D.Systems.BugReports
{
    /// <summary>
    /// The last warnings and errors this process logged, kept from startup so a report can carry them
    /// (<c>sandbox-server.md</c> §7: many bugs only happen on the player's machine). Info lines are
    /// skipped; a stack trace adds its first frame only.
    /// </summary>
    public static class BugReportLogTail
    {
        private static readonly object Gate = new();
        private static readonly Queue<string> Lines = new();
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Hook()
        {
            lock (Gate)
                Lines.Clear();
            if (_hooked)
                return;
            _hooked = true;
            UnityEngine.Application.logMessageReceivedThreaded += Record;
        }

        /// <summary>Oldest first.</summary>
        public static string[] Snapshot()
        {
            lock (Gate)
                return Lines.ToArray();
        }

        /// <summary>Adds a line as if Unity had logged it; for tests.</summary>
        public static void Record(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                return;

            string line = $"{DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)} [{type}] {condition}";
            if (type is LogType.Exception or LogType.Error && !string.IsNullOrEmpty(stackTrace))
            {
                int newline = stackTrace.IndexOf('\n');
                line += " @ " + (newline > 0 ? stackTrace.Substring(0, newline) : stackTrace).Trim();
            }

            if (line.Length > BugReportText.MaxLogLineLength)
                line = line.Substring(0, BugReportText.MaxLogLineLength);

            lock (Gate)
            {
                Lines.Enqueue(line);
                while (Lines.Count > BugReportText.MaxLogLines)
                    Lines.Dequeue();
            }
        }
    }
}
