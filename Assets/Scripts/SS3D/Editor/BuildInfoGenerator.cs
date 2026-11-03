using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SS3D.Systems.Rounds;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SS3D.Editor
{
    /// <summary>
    /// Writes <see cref="BuildInfo"/> (game version, commit id, release-grouped change log) from git into a
    /// gitignored Resources file: on editor load when it changed, and before every player build.
    /// Player builds also get the version as <see cref="PlayerSettings.bundleVersion"/>. The build writes
    /// ProjectSettings to disk with the stamp, so it is restored and saved again after the build (or on the
    /// next editor tick if the build failed), so ProjectSettings never churns. CI passes <c>-ss3dVersion X.Y.Z</c> to stamp
    /// the release version before its tag exists. Without git the existing file is kept (or none
    /// exists) and the lobby falls back. Rules: Documents/architecture/2026-10_game-versioning.md.
    /// </summary>
    [InitializeOnLoad]
    public sealed class BuildInfoGenerator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public const string AssetPath = "Assets/Content/Systems/UI/Lobby/Resources/" + BuildInfo.ResourceName + ".json";
        public const string VersionArgument = "-ss3dVersion";
        private const int EntryCount = 12;
        private const int ReleaseCount = 4;
        private const char Separator = '\u001f';
        private const char RecordSeparator = '\u001e';

        static BuildInfoGenerator() => EditorApplication.delayCall += () => Generate();

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Without git (e.g. a CI container) the version argument still stamps the player.
            string version = Generate() ?? ArgumentVersion() ?? BuildInfo.Version;
            string original = PlayerSettings.bundleVersion;
            if (version == original) return;

            PlayerSettings.bundleVersion = version;
            _stampedFrom = original;
            // Postprocess doesn't run for a failed build; delayCall fires once the build returns either way.
            EditorApplication.delayCall += RestoreVersion;
            Debug.Log($"[BuildInfo] Stamped player version {version}");
        }

        public void OnPostprocessBuild(BuildReport report) => RestoreVersion();

        /// <summary>The bundleVersion before the stamp; null when nothing is stamped.</summary>
        private static string _stampedFrom;

        private static void RestoreVersion()
        {
            if (_stampedFrom == null) return;
            PlayerSettings.bundleVersion = _stampedFrom;
            _stampedFrom = null;
            // The build already wrote the stamped value to ProjectSettings.asset; write the original back.
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SS3D/Build/Regenerate Build Info")]
        private static void GenerateFromMenu() => Generate();

        /// <summary>Regenerates the file; returns the version, or null when git is unavailable.</summary>
        public static string Generate()
        {
            if (!TryGit("rev-parse --short=7 HEAD", out string commit)) return null;

            string version = ComputeVersion();
            BuildInfo.Entry[] entries = ChangelogEntries(version);
            string repository = TryGit("remote get-url origin", out string remote) ? BuildInfo.RepositoryWebUrl(remote) : null;
            string json = JsonUtility.ToJson(new BuildInfo.Data
            {
                version = version,
                commit = commit,
                repository = repository ?? string.Empty,
                entries = entries,
            }, true);
            if (File.Exists(AssetPath) && File.ReadAllText(AssetPath) == json) return version;
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);
            File.WriteAllText(AssetPath, json);
            AssetDatabase.ImportAsset(AssetPath);
            return version;
        }

        /// <summary>
        /// The newest <see cref="EntryCount"/> first-parent changes, each labelled with the release tag
        /// that first contains it. Changes after the newest tag are unreleased, unless this build is
        /// itself a release (CI stamps the version before the tag exists), in which case they carry it.
        /// </summary>
        private static BuildInfo.Entry[] ChangelogEntries(string version)
        {
            List<string> tags = TryGit($"tag --list \"{GameVersion.TagGlob}\" --merged HEAD --sort=-v:refname", out string tagList)
                ? tagList.Split('\n').Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Take(ReleaseCount).ToList()
                : new List<string>();

            string pending = !string.IsNullOrEmpty(version) && !version.Contains('-') && !version.Contains('+')
                && !tags.Contains("v" + version)
                ? version
                : string.Empty;

            List<(string release, string range)> groups = new() { (pending, tags.Count > 0 ? $"{tags[0]}..HEAD" : "HEAD") };
            for (int i = 0; i < tags.Count; i++)
            {
                groups.Add((tags[i].TrimStart('v'), i + 1 < tags.Count ? $"{tags[i + 1]}..{tags[i]}" : tags[i]));
            }

            List<BuildInfo.Entry> entries = new();
            foreach ((string release, string range) in groups)
            {
                int remaining = EntryCount - entries.Count;
                if (remaining <= 0) break;

                // Over-fetch: branch-sync merges are dropped by BuildInfo.FromCommit.
                string format = $"%h{Separator}%cs{Separator}%s{Separator}%b{RecordSeparator}";
                if (!TryGit($"log --first-parent -{remaining * 2} --format={format} {range}", out string log)) continue;

                entries.AddRange(log.Split(RecordSeparator)
                    .Select(record => record.Trim('\n', '\r').Split(Separator))
                    .Where(parts => parts.Length == 4)
                    .Select(parts => BuildInfo.FromCommit(parts[0], parts[1], parts[2], parts[3], release))
                    .Where(entry => entry != null)
                    .Take(remaining));
            }

            return entries.ToArray();
        }

        private static string ArgumentVersion()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, VersionArgument);
            return index >= 0 && index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1])
                ? args[index + 1].Trim()
                : null;
        }

        private static string ComputeVersion()
        {
            if (ArgumentVersion() is { } fromArgument) return fromArgument;

            bool dirty = TryGit("status --porcelain --untracked-files=no", out _);
            if (TryGit($"describe --tags --long --abbrev=7 --match \"{GameVersion.TagGlob}\" HEAD", out string describe)
                && GameVersion.FromDescribe(describe, dirty) is { } tagged)
            {
                return tagged;
            }

            return TryGit("rev-list --count HEAD", out string count) && int.TryParse(count, out int commits)
                && TryGit("rev-parse --short=7 HEAD", out string hash)
                ? GameVersion.Untagged(commits, hash, dirty)
                : null;
        }

        private static bool TryGit(string arguments, out string output)
        {
            output = null;
            try
            {
                // CI builds run inside game-ci's container as root against a checkout owned by the
                // runner user; without safe.directory git refuses ("dubious ownership") and the
                // player ships with no change log.
                using Process git = Process.Start(new ProcessStartInfo("git", "-c safe.directory=* " + arguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(UnityEngine.Application.dataPath)!,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (git == null) return false;
                output = git.StandardOutput.ReadToEnd().Trim();
                string error = git.StandardError.ReadToEnd().Trim();
                if (!git.WaitForExit(5000)) return false;
                if (git.ExitCode != 0)
                {
                    // Expected for some calls (no v* tag yet), so only batch builds log it.
                    if (UnityEngine.Application.isBatchMode)
                    {
                        Debug.Log($"[BuildInfo] git {arguments} failed ({git.ExitCode}): {error}");
                    }

                    return false;
                }

                return output.Length > 0;
            }
            catch (Exception exception)
            {
                Debug.Log($"[BuildInfo] git unavailable; keeping existing build info ({exception.Message}).");
                return false;
            }
        }
    }
}
