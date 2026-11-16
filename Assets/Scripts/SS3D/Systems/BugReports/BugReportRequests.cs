using System;
using FishNet.Connection;
using SS3D.Systems.Comms;

namespace SS3D.Systems.BugReports
{
    /// <summary>
    /// The dialog's entry points that don't own it (bare <c>/bug</c> in chat, the Escape menu button) ask
    /// through here, so chat and lobby code needn't know the dialog's host.
    /// </summary>
    public static class BugReportRequests
    {
        /// <summary>Set by the dialog's host while it can open; returns false when it could not.</summary>
        public static Func<bool> OpenHandler { get; set; }

        /// <summary>Opens the bug report dialog; false when no dialog host is running (dedicated server, batch mode).</summary>
        public static bool RequestOpen() => OpenHandler != null && OpenHandler();

        /// <summary>Server: set by <see cref="BugReportIntake"/> while the server runs. Takes connection, ckey and note.</summary>
        public static Func<NetworkConnection, string, string, string> ChatNoteHandler { get; set; }

        /// <summary>Server: files a <c>/bug &lt;note&gt;</c> from chat; returns the confirmation for the player.</summary>
        public static string FileChatNote(NetworkConnection conn, string ckey, string note) =>
            ChatNoteHandler != null ? ChatNoteHandler(conn, ckey, note) : "Bug reports are not available on this server.";
    }

    /// <summary>Parses the <c>/bug &lt;note&gt;</c> chat command.</summary>
    public static class BugCommand
    {
        public const string Usage = "Usage: /bug <what happened>";
        public const int MaxNoteLength = 500;

        /// <summary>True for any <c>/bug</c> message; <paramref name="note"/> is empty when none was given.</summary>
        public static bool TryParse(string text, out string note)
        {
            note = null;
            if (!CommsComposePrefix.TrySplit(text, out string token, out string body) || token != "bug")
                return false;

            note = body.Trim();
            if (note.Length > MaxNoteLength)
                note = note.Substring(0, MaxNoteLength);
            return true;
        }
    }
}
