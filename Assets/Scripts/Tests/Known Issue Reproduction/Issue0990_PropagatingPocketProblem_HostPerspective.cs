using NUnit.Framework;
using SS3D.Core.Settings;
using System.Collections;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    public class Issue0990_PropagatingPocketProblem_HostPerspective : SpessPlayModeTest
    {

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            if (!setUpOnce)
            {
                yield return LoadAndSetInLobby(NetworkType.Host);
                setUpOnce = true;
            }
            yield return SetInGame();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
        }

        [UnityTest]
        public IEnumerator PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne()
        {
            yield return IssueReproduction.Issue0990_PlayerHasTheSameNumberOfPocketsAfterEndingRoundAndStartingNewOne();
        }

        protected override bool UseMockUpInputs()
        {
            return false;
        }
    }
}
