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
    public class ClientLateJoinActions : SpessClientPlayModeTest
    {
        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();
            yield return TestHelpers.LateJoinRound();
            yield return GetHumanoidController();
            yield return GetInteractionController();
        }

        public override void Setup()
        {
            base.Setup();

            leftMouseClick.performed += InteractionController.HandleRunPrimary;
            leftMouseClick.Enable();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            yield return base.UnityTearDown();
        }

        [UnityTest]
        public IEnumerator PlayerRemainsAboveStationLevelAfterSpawn()
        {
            yield return PlaymodeTestRepository.PlayerRemainsAboveStationLevelAfterSpawn(HumanoidController);
        }
    }
}
