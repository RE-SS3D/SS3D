using System.Collections;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    public class Issue0990_PropagatingPocketProblem_ClientPerspective : SpessClientPlayModeTest
    {

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.StartAndEnterRound();
            yield return GetHumanoidController();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
        }

        [UnityTest]
        public IEnumerator PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne()
        {
            yield return IssueReproduction.Issue0990_PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne();
        }
    }
}
