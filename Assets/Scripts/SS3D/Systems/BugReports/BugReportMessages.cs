using FishNet.Broadcast;

namespace SS3D.Systems.BugReports
{
    /// <summary>Client → server: a report filed from the bug report dialog (<c>sandbox-server.md</c> §7).</summary>
    public struct BugReportSubmitMessage : IBroadcast
    {
        public string Summary;
        public string Details;
        /// <summary>"", "minor", "major" or "blocker"; blank means a developer triages it.</summary>
        public string Severity;
        public bool Crashed;
        /// <summary>What the client saw when the dialog opened; the server stamps its own ckey, round and state.</summary>
        public BugReportClientInfo Client;
        /// <summary>Reduced-size JPEG, or empty when the player removed the screenshot.</summary>
        public byte[] ScreenshotJpg;
        /// <summary>The tail of the player's own log: warnings and errors only.</summary>
        public string[] LogTail;
    }

    /// <summary>Server → client: what happened to a submitted report.</summary>
    public struct BugReportResultMessage : IBroadcast
    {
        public bool Saved;
        public int RoundId;
        /// <summary>Seconds until the player may file another report; 0 when they may now.</summary>
        public float RetryAfterSeconds;
        public string Message;
    }
}
