using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: rapid and double start/stop races in the round single-flight (cancellation, despawn, restart) · Not cheaper because: needs real UniTask cancellation and FishNet despawns; the pure gate is RoundRequestRulesTests
    [Category(TestCategories.Lifecycle)]
    [Category(TestCategories.Nightly)]
    public class RoundLifecycle_DoubleStartStop_HostPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return LoadAndSetInLobby(NetworkType.Host);
            RoundLifecycleTestHelpers.StartObserving();
            ServerHelpers.SetPlayerReadiness("john", true);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            ServerHelpers.ChangeRoundState(false);
            yield return RoundLifecycleTestHelpers.WaitUntilStopped(30f);
            RoundLifecycleTestHelpers.StopObserving();
        }

        [UnityTest]
        public IEnumerator RapidStartThenStopEventuallyReachesStopped()
        {
            yield return LifecycleReproduction.RapidStartThenStopEventuallyReachesStopped();
        }

        [UnityTest]
        public IEnumerator DoubleStartCanBeStoppedCleanly()
        {
            yield return LifecycleReproduction.DoubleStartCanBeStoppedCleanly();
        }

        [UnityTest]
        public IEnumerator RoundCanRestartAfterRapidStartStop()
        {
            yield return LifecycleReproduction.RoundCanRestartAfterRapidStartStop();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
