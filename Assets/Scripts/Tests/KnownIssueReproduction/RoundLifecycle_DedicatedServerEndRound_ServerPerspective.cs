using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [Category(TestCategories.Lifecycle)]
    public class RoundLifecycle_DedicatedServerEndRound_ServerPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInLobby(NetworkType.DedicatedServer);
            RoundLifecycleTestHelpers.StartObserving();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            ServerHelpers.ChangeRoundState(false);
            yield return RoundLifecycleTestHelpers.WaitUntilStopped(30f);
            RoundLifecycleTestHelpers.StopObserving();
        }

        [UnityTest]
        public IEnumerator GamemodeEndRoundStopsActiveRound()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LifecycleReproduction.GamemodeEndRoundStopsActiveRound();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
