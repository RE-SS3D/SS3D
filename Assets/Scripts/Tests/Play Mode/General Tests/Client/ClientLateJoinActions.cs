using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Rounds;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Coimbra;
using System.Diagnostics;
using FishNet.Managing;
using FishNet;

namespace SS3D.Tests
{
    /// <summary>
    /// Set up client to late join the server, necessary as some functionnalities behave 
    /// differently when late joining.
    /// </summary>
    public class ClientLateJoinActions : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInGame(NetworkType.Client, 8f);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.FinishAndExitRound();
            KillAllBuiltExecutables();
        }

        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(HumanoidController);
        }

        /// <summary>
        /// Test that spawn an item and check if the player can drop it and pick it up with primary interaction.
        [UnityTest]
        public IEnumerator PlayerCanDropAndPickUpItem()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return PlaymodeTestRepository.PlayerCanDropAndPickUpItem(this);
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }
    }
}
