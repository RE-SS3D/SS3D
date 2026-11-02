using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace SS3D.Launcher
{
    /// <summary>
    /// Finds the newest stable numbered GitHub release with a download for this platform, using the
    /// same channel rules as the updater scripts (no drafts, prereleases or nightlies). Only the first
    /// page of releases is read; numbered releases are recent, so that is enough for the banner.
    /// </summary>
    public static class ReleaseCheck
    {
        public const string ReleasesApi = "https://api.github.com/repos/henkhooft/SS3D/releases?per_page=100";
        public const string ReleasesPage = "https://github.com/henkhooft/SS3D/releases/latest";
        private static readonly Regex Tag = new(@"^v(\d+)\.(\d+)\.(\d+)$");
        private static readonly Regex Local = new(@"^(\d+)\.(\d+)\.(\d+)(-[^+]*)?(\+.*)?$");

        /// <summary>Release asset name for this platform, or null where no updater exists (macOS, editor-only targets).</summary>
        public static string PlatformAssetName(string tag)
        {
#if UNITY_STANDALONE_WIN
            return $"SS3D-Windows-{tag}.zip";
#elif UNITY_STANDALONE_LINUX
            return $"SS3D-Linux-Client-{tag}.zip";
#else
            return null;
#endif
        }

        /// <summary>Runs the request; calls back with the newer stable tag (e.g. "v0.5.0") or null.</summary>
        public static IEnumerator Run(string localVersion, Action<string> done)
        {
            using UnityWebRequest request = UnityWebRequest.Get(ReleasesApi);
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.timeout = 15;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[MainMenu] Update check failed: {request.error}");
                done(null);
                yield break;
            }

            string latest = LatestStable(request.downloadHandler.text, PlatformAssetName);
            done(latest != null && IsNewer(latest, localVersion) ? latest : null);
        }

        public static string LatestStable(string json, Func<string, string> assetName)
        {
            ReleaseList list;
            try
            {
                list = JsonUtility.FromJson<ReleaseList>("{\"items\":" + json + "}");
            }
            catch (ArgumentException)
            {
                return null;
            }

            string best = null;
            foreach (Release release in list?.items ?? Array.Empty<Release>())
            {
                if (release == null || release.draft || release.prerelease || !Tag.IsMatch(release.tag_name ?? ""))
                {
                    continue;
                }

                string asset = assetName(release.tag_name);
                if (asset == null || Array.FindIndex(release.assets ?? Array.Empty<Asset>(), a => a?.name == asset) < 0)
                {
                    continue;
                }

                if (best == null || Compare(Parse(Tag, release.tag_name), Parse(Tag, best)) > 0)
                {
                    best = release.tag_name;
                }
            }

            return best;
        }

        /// <summary>
        /// Whether stable <paramref name="tag"/> is newer than this build. A dev build of the same
        /// version (<c>0.4.2-dev.3+sha</c>) is older than the release; an unparsable local version
        /// (<c>0.0.0-editor</c> is parsable but never released) is treated as up to date.
        /// </summary>
        public static bool IsNewer(string tag, string localVersion)
        {
            Match local = Local.Match(localVersion ?? "");
            if (!Tag.IsMatch(tag ?? "") || !local.Success || localVersion.StartsWith("0.0.0"))
            {
                return false;
            }

            int compare = Compare(Parse(Tag, tag), Parse(Local, localVersion));
            return compare > 0 || (compare == 0 && local.Groups[4].Success);
        }

        private static int[] Parse(Regex regex, string text)
        {
            Match match = regex.Match(text);
            return new[] { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) };
        }

        private static int Compare(int[] a, int[] b)
        {
            for (int i = 0; i < 3; i++)
            {
                if (a[i] != b[i])
                {
                    return a[i].CompareTo(b[i]);
                }
            }

            return 0;
        }

        // JsonUtility field names match GitHub's JSON keys.
#pragma warning disable SA1300, IDE1006, SA1307
        [Serializable]
        private sealed class ReleaseList
        {
            public Release[] items;
        }

        [Serializable]
        private sealed class Release
        {
            public string tag_name;
            public bool draft;
            public bool prerelease;
            public Asset[] assets;
        }

        [Serializable]
        private sealed class Asset
        {
            public string name;
        }
#pragma warning restore SA1300, IDE1006, SA1307
    }
}
