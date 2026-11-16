using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: GamemodeSubSystem.EndRound stops an Ongoing round on a dedicated server, without a client broadcast · Not cheaper because: needs the gamemode → RequestRoundEnd → StopRound path on a live server
    [Category(TestCategories.Lifecycle)]
    [Category(TestCategories.Nightly)]
    public class RoundLifecycle_DedicatedServerEndRound_ServerPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return LoadAndSetInLobby(NetworkType.DedicatedServer);
            RoundLifecycleTestHelpers.StartObserving();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            ServerHelpers.ChangeRoundState(false);
            yield return RoundLifecycleTestHelpers.WaitUntilStopped(30f);
            RoundLifecycleTestHelpers.StopObserving();
        }

        [UnityTest]
        public IEnumerator GamemodeEndRoundStopsActiveRound()
        {
            yield return LifecycleReproduction.GamemodeEndRoundStopsActiveRound();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
