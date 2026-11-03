using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
  // Guards: Boot → Intro → network session → Game for a Play Mode host, which every PlayMode fixture needs · Not cheaper because: the flow is scene loading and FishNet startup
  [Category(TestCategories.Prerequisites)]
  public class PlayModePrerequisites : PlayModeTest
  {
    [UnityTest]
    [Order(-100)]
    public IEnumerator HostSession_ReachesLobby()
    {
      yield return LoadAndSetInLobby(NetworkType.Host);
      Assert.AreEqual("Game", SceneManager.GetActiveScene().name);
    }

    protected override bool UseMockUpInputs() => false;
  }
}
