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

namespace SS3D.Tests
{
    public class ClientLobbyActions : SpessPlayModeTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create new settings so that tests are run as client-only.
            ApplicationSettings originalSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
            ApplicationSettings newSettings = ScriptableObject.Instantiate(originalSettings);
            newSettings.NetworkType = NetworkType.Client;
            newSettings.Ckey = "testbot";

            // Apply the new settings
            ScriptableSettings.SetOrOverwrite<ApplicationSettings>(newSettings);

            // Start up the client game.
            SceneManager.LoadScene("Startup", LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator ReadyToggleButtonCorrectlyFunctionsWhenClicked()
        {
            yield return ShortWait();

            // ARRANGE
            // Get the button that we need to press
            string ButtonName = "Ready";
            GameObject go = GameObject.Find(ButtonName);
            LabelButton button = go?.GetComponent<LabelButton>();
            Assert.IsNotNull(button, $"Did not find a button in the scene called {ButtonName}");

            // Check the number of players currently ready in the game
            ReadyPlayersSystem readyPlayersSystem = SystemLocator.Get<ReadyPlayersSystem>();
            int originalReadyPlayers = readyPlayersSystem.Count;

            // ACT #1 - Press the button, then wait for a moment
            button.Press();
            yield return ShortWait();

            // ASSERT #1 - We should now be a ready player, so the list should have incremented.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers + 1);

            // ACT #2 - Press the button, then wait for a moment
            button.Press();
            yield return ShortWait();

            // ASSERT #2 - We should be back to having the original number of ready players.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers);

        }

        private IEnumerator ShortWait()
        {
            yield return new WaitForSeconds(3f);
        }

        private IEnumerator LongWait()
        {
            yield return new WaitForSeconds(10f);
        }
    }
}
