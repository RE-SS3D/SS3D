#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Builds a headless Linux dedicated server (<see cref="StandaloneBuildSubtarget.Server"/>).
    /// Invoked from CI via <c>-buildMethod SS3D.Editor.ServerBuildScript.BuildServer</c>, reading the
    /// output path from the <c>-customBuildPath</c> command line argument (the convention used by
    /// game-ci/unity-builder).
    /// </summary>
    public static class ServerBuildScript
    {
        public static void BuildServer()
        {
            string buildPath = GetCommandLineArgument("-customBuildPath");

            if (string.IsNullOrEmpty(buildPath))
            {
                throw new ArgumentException("Missing -customBuildPath command line argument.");
            }

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
