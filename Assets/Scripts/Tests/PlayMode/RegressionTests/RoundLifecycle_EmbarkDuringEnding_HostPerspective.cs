using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: no late embark while the round is Ending (EntitySubSystem.SpawnLatePlayer) · Not cheaper because: the gate reads the live round state during the Ending delay of a running round
    [Category(TestCategories.Lifecycle)]
    [Category(TestCategories.Nightly)]
    public class RoundLifecycle_EmbarkDuringEnding_HostPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return LoadAndSetInLobby(NetworkType.Host);
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
        public IEnumerator LateEmbarkRejectedDuringEndingState()
        {
            yield return LifecycleReproduction.LateEmbarkRejectedDuringEndingState();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
