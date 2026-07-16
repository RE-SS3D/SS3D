#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;

namespace SS3D.Editor
{
    /// <summary>
    /// Builds a regular Linux client (Standalone subtarget, i.e. not <see cref="StandaloneBuildSubtarget.Server"/>).
    /// Sits next to <see cref="ServerBuildScript"/> under the same "SS3D/Build" menu so both builds are one
    /// click each, with no Build Settings toggling required.
    /// </summary>
    public static class ClientBuildScript
    {
        private const string DefaultBuildPath = "Builds/Game/SS3D.x86_64";

        [MenuItem("SS3D/Build/Client (Linux)")]
        public static void BuildClientFromMenu()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultBuildPath) ?? string.Empty);

            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Default;

            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = DefaultBuildPath,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Default,
                options = BuildOptions.None,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Client build failed with result: {report.summary.result}");
            }

            EditorUtility.RevealInFinder(DefaultBuildPath);
        }
    }
}
#endif
