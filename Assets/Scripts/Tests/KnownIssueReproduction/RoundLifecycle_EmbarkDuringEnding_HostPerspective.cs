using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [Category(TestCategories.Lifecycle)]
    public class RoundLifecycle_EmbarkDuringEnding_HostPerspective : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInLobby(NetworkType.Host);
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
        public IEnumerator LateEmbarkRejectedDuringEndingState()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LifecycleReproduction.LateEmbarkRejectedDuringEndingState();
        }

        protected override bool UseMockUpInputs() => false;
    }
}
