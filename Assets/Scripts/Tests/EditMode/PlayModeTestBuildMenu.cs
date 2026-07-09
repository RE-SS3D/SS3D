#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
  public static class PlayModeTestBuildMenu
  {
    private const string MenuPath = "SS3D/Testing/Build Player For PlayMode Tests";

    [MenuItem(MenuPath)]
    public static void BuildPlayerForPlayModeTests()
    {
      string buildDirectory = CompiledBuildPaths.GetBuildDirectory();
      Directory.CreateDirectory(buildDirectory);

      string locationPath = Path.Combine(buildDirectory, CompiledBuildPaths.GetExecutableFileName());
      string[] scenes = EditorBuildSettings.scenes
        .Where(scene => scene.enabled)
        .Select(scene => scene.path)
        .ToArray();

      BuildPlayerOptions options = new BuildPlayerOptions
      {
        scenes = scenes,
        locationPathName = locationPath,
        target = EditorUserBuildSettings.activeBuildTarget,
        options = BuildOptions.Development,
      };

      BuildReport report = BuildPipeline.BuildPlayer(options);

      if (report.summary.result == BuildResult.Succeeded)
      {
        Debug.Log($"PlayMode test build succeeded: {locationPath}");
      }
      else
      {
        Debug.LogError($"PlayMode test build failed: {report.summary.result}");
      }
    }
  }
}
#endif
