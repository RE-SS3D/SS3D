using SS3D.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace SS3D.Permissions
{
    /// <summary>
    /// Stops a remote player from claiming an admin name. Names are self-declared (<c>-ckey=</c>),
    /// so on a public server anyone could join as the owner. A server-only file,
    /// <c>Config/admin-tokens.txt</c>, holds one <c>&lt;name&gt; &lt;token&gt;</c> line per admin;
    /// the admin launches with <c>-token=&lt;token&gt;</c>. It is never synced to clients.
    /// <para>
    /// Enforced on dedicated servers, and on any server that has the file. A listen host on a
    /// LAN without the file keeps the old behaviour, and the host's own connection is never checked.
    /// </para>
    /// </summary>
    public static class AdminTokens
    {
        public const string FileName = "admin-tokens.txt";

        public static string FilePath => Paths.GetPath(GamePaths.Config, true) + "/" + FileName;

        /// <summary>
        /// Whether a remote login may use <paramref name="ckey"/>, which holds <paramref name="role"/>.
        /// </summary>
        public static bool IsAdmitted(ServerRoleTypes role, string ckey, string presentedToken, bool isDedicatedServer,
            out string reason)
        {
            bool fileExists = File.Exists(FilePath);
            IReadOnlyDictionary<string, string> tokens = fileExists
                ? Parse(File.ReadAllLines(FilePath))
                : new Dictionary<string, string>();
            return IsAdmitted(role, ckey, presentedToken, isDedicatedServer || fileExists, tokens, out reason);
        }

        public static bool IsAdmitted(ServerRoleTypes role, string ckey, string presentedToken, bool enforce,
            IReadOnlyDictionary<string, string> tokens, out string reason)
        {
            reason = null;
            if (role < ServerRoleTypes.Administrator || !enforce)
                return true;

            if (!tokens.TryGetValue(ckey ?? string.Empty, out string expected))
            {
                reason = $"'{ckey}' is an admin name with no line in {FileName}";
                return false;
            }

            if (!FixedTimeEquals(expected, presentedToken))
            {
                reason = $"wrong or missing -token for admin name '{ckey}'";
                return false;
            }

            return true;
        }

        public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
        {
            Dictionary<string, string> tokens = new(StringComparer.Ordinal);
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string[] words = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                    tokens[words[0]] = words[1];
            }

            return tokens;
        }

        private static bool FixedTimeEquals(string expected, string presented)
        {
            if (string.IsNullOrEmpty(expected) || presented == null)
                return false;

            int diff = expected.Length ^ presented.Length;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ (i < presented.Length ? presented[i] : 0);
            return diff == 0;
        }
    }
}
