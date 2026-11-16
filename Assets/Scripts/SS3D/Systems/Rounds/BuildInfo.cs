using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Game version, commit id and a release-grouped change log, generated from git into a Resources
    /// text asset by the editor (on load and before player builds). The change log lists the
    /// first-parent history of the build — merged pull requests by their PR title — grouped under the
    /// release tag that shipped them (empty <see cref="Entry.release"/> = not released yet). The file
    /// is gitignored; without it the label falls back to <see cref="UnityEngine.Application.version"/>
    /// and the change log is empty.
    /// Versioning rules: Documents/architecture/2026-10_game-versioning.md.
    /// </summary>
    public static class BuildInfo
    {
        public const string ResourceName = "BuildInfo";

        [Serializable]
        public sealed class Entry
        {
            public string hash;
            public string date;
            public string subject;

            /// <summary>Release version that shipped this change ("0.1.1"); empty while unreleased.</summary>
            public string release;

            /// <summary>Pull request number when the change was a merged PR, else 0.</summary>
            public int pr;

            /// <summary>The PR (or, for a direct commit, the commit) page under <paramref name="repository"/>.</summary>
            public string Url(string repository)
            {
                if (string.IsNullOrEmpty(repository)) return null;
                if (pr > 0) return $"{repository}/pull/{pr}";
                return string.IsNullOrEmpty(hash) ? null : $"{repository}/commit/{hash}";
            }
        }

        [Serializable]
        public sealed class Data
        {
            /// <summary>See <see cref="GameVersion"/>, e.g. "0.1.0" or "0.1.1-dev.12+5da58b9".</summary>
            public string version;
            public string commit;

            /// <summary>Web URL of the GitHub repository ("https://github.com/owner/repo"), or empty.</summary>
            public string repository;

            public Entry[] entries = Array.Empty<Entry>();

            /// <summary>The repository's releases page, or null when the repository is unknown.</summary>
            public string ReleasesUrl => string.IsNullOrEmpty(repository) ? null : repository + "/releases";
        }

        private static readonly Regex PullRequestMerge = new(@"^Merge pull request #(\d+) from ", RegexOptions.Compiled);
        private static readonly Regex GitHubRemote = new(
            @"^(?:https://|git@|ssh://git@)github\.com[:/](?<path>[^/]+/[^/]+?)(?:\.git)?/?$", RegexOptions.Compiled);

        /// <summary>
        /// Turns one first-parent commit into a change log entry. GitHub PR merges ("Merge pull request
        /// #94 from …") take the PR title from the first body line; other merges (branch syncs) carry no
        /// change of their own and return null.
        /// </summary>
        public static Entry FromCommit(string hash, string date, string subject, string body, string release)
        {
            subject = subject?.Trim() ?? string.Empty;
            Match pullRequest = PullRequestMerge.Match(subject);
            int pr = 0;
            if (pullRequest.Success)
            {
                pr = int.Parse(pullRequest.Groups[1].Value);
                string title = FirstLine(body);
                subject = string.IsNullOrEmpty(title) ? $"Pull request #{pr}" : title;
            }
            else if (subject.StartsWith("Merge ", StringComparison.Ordinal))
            {
                return null;
            }

            return new Entry { hash = hash, date = date, subject = subject, release = release ?? string.Empty, pr = pr };
        }

        /// <summary>"git@github.com:owner/repo.git" or an https remote → "https://github.com/owner/repo"; null otherwise.</summary>
        public static string RepositoryWebUrl(string remote)
        {
            Match match = GitHubRemote.Match(remote?.Trim() ?? string.Empty);
            return match.Success ? "https://github.com/" + match.Groups["path"].Value : null;
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0) return trimmed;
            }

            return null;
        }

        private static Data _data;

        public static Data Current => _data ??= Load();

        /// <summary>This process's game version; player builds also stamp it into <see cref="UnityEngine.Application.version"/>.</summary>
        public static string Version => string.IsNullOrEmpty(Current.version) ? UnityEngine.Application.version : Current.version;

        /// <summary>
        /// "0.1.1-dev.12+007c4e9" for dev builds (the version already names the commit), "0.1.0 · 007c4e9"
        /// for releases, or the bare version without generated build info.
        /// </summary>
        public static string Label => string.IsNullOrEmpty(Current.commit) || Version.Contains('+')
            ? Version
            : $"{Version} · {Current.commit}";

        public static Data Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Data();
            try
            {
                Data data = JsonUtility.FromJson<Data>(json) ?? new Data();
                data.entries ??= Array.Empty<Entry>();
                return data;
            }
            catch (ArgumentException)
            {
                return new Data();
            }
        }

        private static Data Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourceName);
            return Parse(asset != null ? asset.text : null);
        }
    }
}
