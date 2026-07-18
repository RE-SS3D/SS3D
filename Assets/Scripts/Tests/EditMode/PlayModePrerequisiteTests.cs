#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Tests;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
  /// <summary>
  /// Fast prerequisite checks and optional player build for PlayMode tests.
  /// </summary>
  public class PlayModePrerequisiteTests
  {
    [Test]
    [Order(-1000)]
    [Category(TestCategories.Prerequisites)]
    public void CompiledBuild_ExistsForExternalProcessTests()
    {
      if (!CompiledBuildPaths.HasCompiledBuild)
      {
        // Ignore (not Inconclusive): Unity Test Framework exits non-zero on inconclusive,
        // which fails game-ci EditMode CI even when every real assertion passed.
        Assert.Ignore(
          $"{CompiledBuildPaths.MissingBuildMessage} " +
          "Run SS3D/Testing/Build Player For PlayMode Tests or execute the Explicit test BuildCompiledPlayerForPlayModeTests.");
      }
    }

    [Test]
    [Explicit]
    [Category(TestCategories.Prerequisites)]
    [Timeout(900000)]
    public void BuildCompiledPlayerForPlayModeTests()
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
      Assert.AreEqual(BuildResult.Succeeded, report.summary.result, BuildSummary(report));
      Assert.IsTrue(File.Exists(locationPath), $"Expected build output at {locationPath}");
    }

    private static string BuildSummary(BuildReport report)
    {
      if (report.summary.result == BuildResult.Succeeded)
      {
        return report.summary.ToString();
      }

      return string.Join("\n", report.steps.SelectMany(step => step.messages).Select(message => message.content));
    }
  }
}
#endif
