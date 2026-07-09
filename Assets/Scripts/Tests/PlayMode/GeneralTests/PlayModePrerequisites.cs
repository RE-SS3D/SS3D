using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
  [Category(TestCategories.Prerequisites)]
  public class PlayModePrerequisites : PlayModeTest
  {
    [UnityTest]
    [Order(-100)]
    public IEnumerator HostSession_ReachesLobby()
    {
      LogAssert.ignoreFailingMessages = true;
      yield return LoadAndSetInLobby(NetworkType.Host);
      Assert.AreEqual("Game", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    [Order(-90)]
    [Category(TestCategories.RequiresCompiledBuild)]
    public IEnumerator CompiledBuild_IsAvailableForExternalProcessTests()
    {
      LogAssert.ignoreFailingMessages = true;
      if (!CompiledBuildPaths.HasCompiledBuild)
      {
        Assert.Ignore(CompiledBuildPaths.MissingBuildMessage);
      }

      yield return null;
    }

    protected override bool UseMockUpInputs() => false;
  }
}
