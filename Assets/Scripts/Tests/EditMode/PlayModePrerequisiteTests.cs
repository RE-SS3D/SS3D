#if UNITY_EDITOR
using NUnit.Framework;
using SS3D.Tests;

namespace SS3D.Tests.EditMode
{
  /// <summary>
  /// Fast prerequisite check that a player build exists for tests that launch external processes.
  /// Build via <c>SS3D → Build → Client (Linux)</c> (or Client + Dedicated Server).
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
        Assert.Ignore(CompiledBuildPaths.MissingBuildMessage);
      }
    }
  }
}
#endif
