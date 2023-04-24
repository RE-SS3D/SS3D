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
    /// All tests related to doing stuff in the lobby as a client.
    /// </summary>
    public class ClientLobbyActions : SpessPlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return LoadAndSetInLobby(NetworkType.Client);
        }

        [UnityTest]
        public IEnumerator ReadyToggleButtonCorrectlyFunctionsWhenClicked()
        {
            yield return PlaymodeTestRepository.ReadyToggleButtonCorrectlyFunctionsWhenClicked();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return TestHelpers.FinishAndExitRound();
            KillAllBuiltExecutables();
        }

        protected override bool UseMockUpInputs()
        {
            return false;
        }

    }


}
