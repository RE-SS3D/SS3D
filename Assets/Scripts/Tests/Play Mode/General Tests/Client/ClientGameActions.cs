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

namespace SS3D.Tests
{
    public class ClientGameActions : SpessClientPlayModeTest
    {
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.StartAndEnterRound();
            yield return GetHumanoidController();
            yield return GetInteractionController();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
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
            Debug.Log("player can drop and pick up item beginning");
            yield return PlaymodeTestRepository.PlayerCanDropAndPickUpItem(this);
        }
    }
}
