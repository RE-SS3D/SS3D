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
    public class ClientLobbyActions : SpessPlayModeTest
    {
        public bool lobbySceneLoaded = false;

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
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Startup", LoadSceneMode.Single);
        }

        private void ClientSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UnityEngine.Debug.Log($"Scene = {scene.name}, Mode = {mode}");
            if (scene.name.Equals("Lobby"))
            {
                lobbySceneLoaded = true;
            }
        }



        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            base.SetUp();
            yield return ShortWait();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            base.TearDown();
            yield return ShortWait();
        }


        [UnityTest]
        public IEnumerator ReadyToggleButtonCorrectlyFunctionsWhenClicked()
        {
            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return null;

            // ARRANGE
            // Get the button that we need to press
            string ButtonName = "Ready";
            GameObject go = GameObject.Find(ButtonName);
            LabelButton button = go?.GetComponent<LabelButton>();
            Assert.IsNotNull(button, $"Did not find a button in the scene called {ButtonName}");

            // Check the number of players currently ready in the game
            ReadyPlayersSystem readyPlayersSystem = SystemLocator.Get<ReadyPlayersSystem>();
            int originalReadyPlayers = readyPlayersSystem.Count;

            // Check the colour of the ready button
            Color originalButtonColor = button.GetComponent<Image>().color;

            // ACT #1 - Press the button to set us to Ready, then wait for a moment
            button.Press();
            yield return ShortWait();

            // ASSERT #1: Player should now be Ready.
            // Check: The ready player count should have incremented.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers + 1, $"Number of ready players was not incremented when this player became ready.");
            // Check: The ready button color should have changed.
            Assert.IsTrue(originalButtonColor != button.GetComponent<Image>().color, $"The button colour did not change when the button was clicked");


            // ACT #2 - Press the button to set us to Not Ready, then wait for a moment
            button.Press();
            yield return ShortWait();

            // ASSERT #2: Player should now be Not Ready
            // Check: The ready player count should be back to having the original number of ready players.
            Assert.IsTrue(readyPlayersSystem.Count == originalReadyPlayers, $"Number of ready players was not decremented when this player became not ready.");
            // Check: The ready button color should have changed back to the original.
            Assert.IsTrue(originalButtonColor == button.GetComponent<Image>().color, $"The button colour did not change when the button was clicked");
        }

        private IEnumerator ShortWait()
        {
            yield return new WaitForSeconds(2f);
        }

        private IEnumerator LongWaitForServerToLoad()
        {
            yield return new WaitForSeconds(10f);
        }

        private IEnumerator WaitUntil(bool finished, float timeout = 20f)
        {
            UnityEngine.Debug.Log($"StartTime = {Time.time}");
            float startTime = Time.time;
            while (!finished && Time.time < startTime + timeout)
            {
                yield return null;
            }
        }

    }
}
