using System;
using System.Text;

namespace SS3D.Systems.Rounds
{
    /// <summary>Admin-authored server identity shown on the lobby Server Info tab.</summary>
    [Serializable]
    public sealed class SavedServerInfo
    {
        public string serverName;
        public string motd;
    }

    /// <summary>Pure validation for server name / MOTD text (EditMode-tested).</summary>
    public static class ServerInfoText
    {
        public const int MaxNameLength = 64;
        public const int MaxMotdLength = 1000;
        public const string DefaultName = "SS3D Server";
        public const string DefaultMotd = "Welcome aboard. This is a community-maintained pre-alpha build — expect things to break, and report what you find.";

        /// <summary>Single line, no control characters, trimmed and clamped; empty becomes the default.</summary>
        public static string SanitizeName(string name)
        {
            string result = Clean(name, MaxNameLength, allowNewlines: false);
            return result.Length == 0 ? DefaultName : result;
        }

        /// <summary>Keeps paragraph breaks (at most two in a row), strips other control characters; empty becomes the default.</summary>
        public static string SanitizeMotd(string motd)
        {
            string result = Clean(motd, MaxMotdLength, allowNewlines: true);
            return result.Length == 0 ? DefaultMotd : result;
        }

        private static string Clean(string text, int maxLength, bool allowNewlines)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            StringBuilder builder = new(Math.Min(text.Length, maxLength));
            int newlines = 0;
            foreach (char raw in text.Replace("\r\n", "\n").Replace('\r', '\n'))
            {
                char c = raw;
                if (c == '\n' && allowNewlines)
                {
                    if (builder.Length == 0 || ++newlines > 2) continue;
                    while (builder.Length > 0 && builder[^1] == ' ') builder.Length--;
                    builder.Append('\n');
                    continue;
                }

                if (char.IsControl(c) || char.IsWhiteSpace(c)) c = ' ';
                if (c == ' ' && (builder.Length == 0 || builder[^1] == ' ' || builder[^1] == '\n')) continue;
                newlines = 0;
                builder.Append(c);
                if (builder.Length >= maxLength) break;
            }

            return builder.ToString().Trim();
        }
    }
}
