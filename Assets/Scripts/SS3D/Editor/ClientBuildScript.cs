#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SS3D.Editor
{
    /// <summary>
    /// Builds a regular Linux client (Standalone subtarget, i.e. not <see cref="StandaloneBuildSubtarget.Server"/>).
    /// Sits next to <see cref="ServerBuildScript"/> under the same "SS3D/Build" menu so both builds are one
    /// click each, with no Build Settings toggling required.
    /// <para>
    /// From CI, invoked via <c>-buildMethod SS3D.Editor.ClientBuildScript.BuildClient</c>, reading the
    /// output path from the <c>-customBuildPath</c> command line argument, same convention as
    /// <see cref="ServerBuildScript.BuildServer()"/>.
    /// </para>
    /// </summary>
    public static class ClientBuildScript
    {
        private const string DefaultBuildPath = "Builds/Game/SS3D.x86_64";

        [MenuItem("SS3D/Build/Client (Linux)")]
        public static void BuildClientFromMenu()
        {
            BuildClient(DefaultBuildPath);
            EditorUtility.RevealInFinder(DefaultBuildPath);
        }

        public static void BuildClient()
        {
            string buildPath = GetCommandLineArgument("-customBuildPath");

            if (string.IsNullOrEmpty(buildPath))
            {
                throw new ArgumentException("Missing -customBuildPath command line argument.");
            }

            BuildClient(buildPath);
        }

        /// <summary>
        /// Builds the client to <paramref name="buildPath"/>. Used by the menu item, CI
        /// (<see cref="BuildClient()"/>), and <c>SS3D/Build/Client + Dedicated Server (Linux)</c>.
        /// </summary>
        public static void BuildClient(string buildPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(buildPath) ?? string.Empty);

            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = buildPath,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception($"Client build failed with result: {report.summary.result}");
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
