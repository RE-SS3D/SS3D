using System.Collections;
using NUnit.Framework;
using SS3D.Networking;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// PlayMode regression for the interaction hardening effort.
    /// Priority, intent filtering, and RPC identifier resolution are covered in <see cref="EditorTests.InteractionPipelineTests"/>.
    /// </summary>
    [Category(TestCategories.RequiresCompiledBuild)]
    public class InteractionPlayModeTests : PlayModeTest
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
            KillAllBuiltExecutables();
        }

        [UnityTest]
        public IEnumerator PlayerCanDropAndPickUpItem_InteractionPipelineRegression()
        {
            yield return PlaymodeTestRepository.PlayerCanDropAndPickUpItem(this);
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }
    }
}
