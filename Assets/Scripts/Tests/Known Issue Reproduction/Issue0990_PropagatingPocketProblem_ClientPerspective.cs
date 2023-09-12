using SS3D.Core.Settings;
using System.Collections;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    public class Issue0990_PropagatingPocketProblem_ClientPerspective : SpessPlayModeTest
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
            yield return TestHelpers.FinishAndExitRound();
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
