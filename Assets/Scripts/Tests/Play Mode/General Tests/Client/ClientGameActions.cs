using System;
using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.TestTools;
using UnityEngine.Windows;
using SS3D.Systems.Inventory.Containers;
using System.Security.Cryptography;
using SS3D.Core.Settings;

namespace SS3D.Tests
{
    public class ClientGameActions : SpessPlayModeTest
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

        /// <summary>
        /// Test which confirms that the player can correctly move in each of the eight directions.
        /// Note: this test is vulnerable to the player being blocked from movement by map features.
        /// </summary>
        /// <param name="controller">The player character.</param>
        [UnityTest]
        public IEnumerator PlayerCanMoveInEachDirectionCorrectly()
        {
            yield return PlaymodeTestRepository.PlayerCanMoveInEachDirectionCorrectly(this, HumanoidController);
        }

        /// <summary>
        /// Test that spawn an item and check if the player can drop it and pick it up with primary interaction.
        [UnityTest]
        public IEnumerator PlayerCanDropAndPickUpItem()
        {
            yield return PlaymodeTestRepository.PlayerCanDropAndPickUpItem(this);
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }
    }
}
