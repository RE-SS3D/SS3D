using System.Text.RegularExpressions;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Formats the game version from git (see Documents/architecture/2026-10_game-versioning.md).
    /// Release tags are <c>vMAJOR.MINOR.PATCH</c>; a build on a tag is that version, a build after it
    /// is the next patch's dev pre-release: <c>0.1.1-dev.12+5da58b9</c>. Mirrors
    /// <c>Tools/versioning/next_version.py describe</c>.
    /// </summary>
    public static class GameVersion
    {
        /// <summary>Glob for <c>git describe --match</c>; upstream's unprefixed tags never match.</summary>
        public const string TagGlob = "v[0-9]*";

        private static readonly Regex DescribePattern = new(@"^v(\d+)\.(\d+)\.(\d+)-(\d+)-g([0-9a-f]+)$");

        /// <summary>
        /// From <c>git describe --tags --long --abbrev=7 --match v[0-9]*</c> output such as
        /// <c>v0.1.0-12-g5da58b9</c>. Returns null when the text is not a release-tag description.
        /// </summary>
        public static string FromDescribe(string describe, bool dirty)
        {
            Match match = DescribePattern.Match(describe?.Trim() ?? "");
            if (!match.Success)
            {
                return null;
            }

            int major = int.Parse(match.Groups[1].Value);
            int minor = int.Parse(match.Groups[2].Value);
            int patch = int.Parse(match.Groups[3].Value);
            int commits = int.Parse(match.Groups[4].Value);
            string hash = match.Groups[5].Value;

            return commits == 0
                ? $"{major}.{minor}.{patch}" + (dirty ? $"+{hash}.dirty" : "")
                : Dev(major, minor, patch + 1, commits, hash, dirty);
        }

        /// <summary>Before the first release tag exists, builds are pre-releases of it: <c>0.1.0-dev.N+hash</c>.</summary>
        public static string Untagged(int commitCount, string hash, bool dirty) => Dev(0, 1, 0, commitCount, hash, dirty);

        private static string Dev(int major, int minor, int patch, int commits, string hash, bool dirty) =>
            $"{major}.{minor}.{patch}-dev.{commits}+{hash}" + (dirty ? ".dirty" : "");
    }
}
