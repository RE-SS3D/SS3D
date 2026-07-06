using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.Networking;
using System.Collections;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [Category(TestCategories.RequiresCompiledBuild)]
    public class Issue0990_PropagatingPocketProblem_ClientPerspective : PlayModeTest
    {

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInGame(NetworkType.Client);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.TryFinishAndExitRound();
            yield return PrepareNetworkTestEnvironment();
        }

        [UnityTest]
        public IEnumerator PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return IssueReproduction.Issue0990_PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne();
        }

        protected override bool UseMockUpInputs()
        {
            return false;
        }
    }
}
