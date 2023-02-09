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
using SS3D.Systems.InputHandling;
using SS3D.Systems.Entities.Humanoid;
using System.Text;

namespace SS3D.Tests
{
    public class ServerGameActions : SpessServerPlayModeTest
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

        }

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            // Set up the test itself
            yield return base.UnitySetUp();
            yield return new WaitForSeconds(3f);

            // Make some clients
            yield return ServerHelpers.CreateClients(1);

            ServerHelpers.SetAllPlayersReady();
            yield return null;
            ServerHelpers.ChangeRoundState(true);
            yield return GetController();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            ServerHelpers.ChangeRoundState(false);
            yield return base.UnityTearDown();

            KillAllBuiltExecutables();
        }

        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn()
        {
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(controller);
        }

        [UnityTest]
        public IEnumerator y()
        {
            yield return PlaymodeTestRepository.PlayerCanMoveInEachDirectionCorrectly(controller);
        }
    }
}
