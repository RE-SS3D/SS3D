#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Builds a headless Linux dedicated server (<see cref="StandaloneBuildSubtarget.Server"/>).
    /// <para>
    /// From CI, invoked via <c>-buildMethod SS3D.Editor.ServerBuildScript.BuildServer</c>, reading the
    /// output path from the <c>-customBuildPath</c> command line argument (the convention used by
    /// game-ci/unity-builder).
    /// </para>
    /// <para>
    /// From the Editor, use the "SS3D/Build/Dedicated Server (Linux)" menu item instead of the regular
    /// Build Settings window - toggling "Server Build" by hand is easy to forget, which silently produces
    /// a normal client build that looks like a server (it still runs, connects, etc.) but never compiles
    /// with UNITY_SERVER, so none of the server-only guards throughout the codebase take effect.
    /// </para>
    /// </summary>
    public static class ServerBuildScript
    {
        private const string DefaultBuildPath = "Builds/GameServer/SS3D.x86_64";

        [MenuItem("SS3D/Build/Dedicated Server (Linux)")]
        public static void BuildServerFromMenu()
        {
            BuildServer(DefaultBuildPath);
            EditorUtility.RevealInFinder(DefaultBuildPath);
        }

        public static void BuildServer()
        {
            string buildPath = GetCommandLineArgument("-customBuildPath");

            if (string.IsNullOrEmpty(buildPath))
            {
                throw new ArgumentException("Missing -customBuildPath command line argument.");
            }

            BuildServer(buildPath);
        }

        /// <summary>
        /// Builds the dedicated server to <paramref name="buildPath"/>. Used by the menu item, CI
        /// (<see cref="BuildServer()"/>), and <c>SS3D/Build/Client + Dedicated Server (Linux)</c>.
        /// </summary>
        public static void BuildServer(string buildPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(buildPath) ?? string.Empty);

            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = buildPath,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception($"Dedicated server build failed with result: {report.summary.result}");
            }
        }

        private static string GetCommandLineArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif
