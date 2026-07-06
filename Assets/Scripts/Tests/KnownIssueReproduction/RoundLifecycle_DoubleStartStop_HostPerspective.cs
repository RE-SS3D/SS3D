using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [Category(TestCategories.Lifecycle)]
    public class RoundLifecycle_DoubleStartStop_HostPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInLobby(NetworkType.Host);
            RoundLifecycleTestHelpers.StartObserving();
            ServerHelpers.SetPlayerReadiness("john", true);
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
        public IEnumerator RapidStartThenStopEventuallyReachesStopped()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LifecycleReproduction.RapidStartThenStopEventuallyReachesStopped();
        }

        [UnityTest]
        public IEnumerator DoubleStartCanBeStoppedCleanly()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LifecycleReproduction.DoubleStartCanBeStoppedCleanly();
        }

        [UnityTest]
        public IEnumerator RoundCanRestartAfterRapidStartStop()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LifecycleReproduction.RoundCanRestartAfterRapidStartStop();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
