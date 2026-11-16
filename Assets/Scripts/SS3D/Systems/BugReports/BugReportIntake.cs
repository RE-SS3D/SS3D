using System;
using System.Globalization;
using System.IO;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.Systems.BugReports
{
    /// <summary>
    /// One line of <c>bugs.jsonl</c>, written by <c>/bug &lt;note&gt;</c> (<see cref="source"/> "chat") or the bug
    /// report dialog ("dialog"). The ckey, round, state and position are the server's view; <see cref="client"/>
    /// is what the player's game saw when the dialog opened.
    /// </summary>
    [Serializable]
    public sealed class BugReportEntry
    {
        public string timestamp;
        public int roundId;
        public string ckey;
        /// <summary>alive, ghost or lobby.</summary>
        public string state;
        public Vector3 position;
        /// <summary>The chat note, or the dialog's summary.</summary>
        public string note;
        public string source;
        public string details;
        /// <summary>"", minor, major or blocker.</summary>
        public string severity;
        public bool crashed;
        /// <summary>File name under <c>ServerMeta/bug-screenshots/</c>, or empty.</summary>
        public string screenshot;
        public BugReportClientInfo client;
        public string[] logTail;
    }

    /// <summary>What happened to a report; <see cref="Message"/> is shown to the player.</summary>
    public readonly struct BugReportOutcome
    {
        public readonly bool Saved;
        public readonly float RetryAfterSeconds;
        public readonly string Message;

        public BugReportOutcome(bool saved, float retryAfterSeconds, string message)
        {
            Saved = saved;
            RetryAfterSeconds = retryAfterSeconds;
            Message = message;
        }
    }

    /// <summary>
    /// Server side of bug reports (<c>2026-11_sandbox-server.md</c> P4; moved out of Rounds by tech-debt P6.5):
    /// <c>/bug &lt;note&gt;</c> from chat and <see cref="BugReportSubmitMessage"/> from the dialog become lines
    /// of <c>bugs.jsonl</c> in <c>ServerMeta</c>, one per player per minute. Chat reaches it through
    /// <see cref="BugReportRequests.ChatNoteHandler"/>, so Comms needn't know it.
    /// </summary>
    public sealed class BugReportIntake : NetworkSubSystem
    {
        private readonly BugReportRateLimiter _rateLimiter = new();
        private Func<NetworkConnection, string, string, string> _chatNoteHandler;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.RegisterBroadcast<BugReportSubmitMessage>(OnServerReceiveBugReport);
            _chatNoteHandler = RecordBugReport;
            BugReportRequests.ChatNoteHandler = _chatNoteHandler;
        }

        public override void OnStopServer()
        {
            if (ServerManager != null)
                ServerManager.UnregisterBroadcast<BugReportSubmitMessage>(OnServerReceiveBugReport);
            if (BugReportRequests.ChatNoteHandler == _chatNoteHandler)
                BugReportRequests.ChatNoteHandler = null;
            base.OnStopServer();
        }

        /// <summary>Server: records a <c>/bug &lt;note&gt;</c> report and returns the confirmation for the player.</summary>
        [Server]
        public string RecordBugReport(NetworkConnection conn, string ckey, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return BugCommand.Usage;

            BugReportEntry entry = new() { note = note, source = "chat" };
            return RecordBugReport(conn, ckey, entry, null).Message;
        }

        /// <summary>
        /// Server: stamps the server's view (time, round, ckey, state, position) on <paramref name="entry"/>,
        /// enforces one report per player per minute, saves the screenshot and appends the line.
        /// </summary>
        [Server]
        public BugReportOutcome RecordBugReport(NetworkConnection conn, string ckey, BugReportEntry entry, byte[] screenshotJpg)
        {
            if (!_rateLimiter.TryConsume(ckey, Time.unscaledTime, out float retryAfter))
            {
                return new BugReportOutcome(false, retryAfter,
                    $"You can send another report in {BugReportText.FormatCountdown(retryAfter)}.");
            }

            RoundSubSystem rounds = SubSystems.GetOrNull<RoundSubSystem>();
            int roundId = rounds != null ? rounds.RoundId : 0;
            string state = "lobby";
            Vector3 position = Vector3.zero;
            if (rounds != null && rounds.CurrentRoundState == RoundState.Ongoing && SubSystems.TryGet(out EntitySubSystem entities)
                && entities.TryGetSpawnedEntity(conn, out Entity entity))
            {
                state = entity.GetComponent<HumanoidGhostController>() != null ? "ghost" : "alive";
                position = entity.transform.position;
            }

            DateTime now = DateTime.UtcNow;
            entry.timestamp = now.ToString("o", CultureInfo.InvariantCulture);
            entry.roundId = roundId;
            entry.ckey = ckey;
            entry.state = state;
            entry.position = position;
            entry.screenshot = SaveBugScreenshot(screenshotJpg, roundId, ckey, now);

            bool saved = LocalStorage.AppendJsonlLine(PersistencePaths.BugReports, JsonUtility.ToJson(entry));
            Log.Information(this, "Bug report ({source}, {severity}) from {ckey} in round {roundId}: {note}", Logs.ServerOnly,
                entry.source, string.IsNullOrEmpty(entry.severity) ? "untriaged" : entry.severity, ckey, roundId, entry.note);
            return saved
                ? new BugReportOutcome(true, BugReportRateLimiter.IntervalSeconds, $"Bug report saved (round {roundId}). Thanks!")
                : new BugReportOutcome(false, 0f, "Your bug report reached the server log but could not be saved to file.");
        }

        /// <summary>Writes the dialog's screenshot beside <c>bugs.jsonl</c>; returns its file name, or empty.</summary>
        private string SaveBugScreenshot(byte[] jpg, int roundId, string ckey, DateTime utc)
        {
            if (jpg == null || jpg.Length == 0 || jpg.Length > BugReportText.MaxScreenshotBytes)
                return "";

            string name = $"{roundId}-{utc:yyyyMMdd-HHmmss}-{SafeName(ckey)}.jpg";
            try
            {
                string folder = Path.GetFullPath(Paths.GetPath(GamePaths.Data, true) + PersistencePaths.BugScreenshots);
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name), jpg);
                return name;
            }
            catch (Exception exception)
            {
                Log.Warning(this, "Could not save bug screenshot: {error}", Logs.ServerOnly, exception.Message);
                return "";
            }
        }

        private static string SafeName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            return text;
        }

        /// <summary>Server: a report from the dialog. The client's text is clamped again; its context is kept as sent.</summary>
        private void OnServerReceiveBugReport(NetworkConnection conn, BugReportSubmitMessage message)
        {
            string ckey = SubSystems.TryGet(out PlayerSubSystem players) ? players.GetCkey(conn) : null;
            if (string.IsNullOrEmpty(ckey))
                return;

            string summary = BugReportText.Clamp(message.Summary, BugReportText.MaxSummaryLength);
            BugReportOutcome outcome;
            if (summary.Length == 0)
            {
                outcome = new BugReportOutcome(false, 0f, "A bug report needs a summary.");
            }
            else
            {
                BugReportEntry entry = new()
                {
                    note = summary,
                    source = "dialog",
                    details = BugReportText.Clamp(message.Details, BugReportText.MaxDetailsLength),
                    severity = BugReportText.SeverityKey(BugReportText.ParseSeverity(message.Severity)),
                    crashed = message.Crashed,
                    client = message.Client,
                    logTail = BugReportText.ClampLog(message.LogTail),
                };
                outcome = RecordBugReport(conn, ckey, entry, message.ScreenshotJpg);
            }

            InstanceFinder.ServerManager.Broadcast(conn, new BugReportResultMessage
            {
                Saved = outcome.Saved,
                RoundId = SubSystems.TryGet(out RoundSubSystem rounds) ? rounds.RoundId : 0,
                RetryAfterSeconds = outcome.RetryAfterSeconds,
                Message = outcome.Message,
            });
        }
    }
}
