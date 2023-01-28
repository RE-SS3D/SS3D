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

namespace SS3D.Tests
{
    public class ClientGameActions : SpessPlayModeTest
    {
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";

        public bool lobbySceneLoaded = false;

        public ScriptedInput input;
        public HumanoidController controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create new settings so that tests are run as client-only.
            ApplicationSettings originalSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
            ApplicationSettings newSettings = ScriptableObject.Instantiate(originalSettings);
            newSettings.NetworkType = NetworkType.Client;
            newSettings.Ckey = "john";

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
            const string ReadyButtonName = "Ready";
            const string ServerSettingsTabName = "Server Settings";
            const string StartRoundButtonName = "Start Round";

            base.SetUp();

            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return ShortWait();

            // Fire up the game
            PressButton(ReadyButtonName); yield return ShortWait();
            SetTabActive(ServerSettingsTabName); yield return ShortWait();
            PressButton(StartRoundButtonName); yield return ShortWait();

            // Create our fake input and assign it to the player
            input = new ScriptedInput();
            UserInput.SetInputService(input);

            // Give a moment's pause before test start
            yield return ShortWait();
            yield return GetController();

        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            base.TearDown();
            yield return ShortWait();
        }

        public IEnumerator GetController()
        {
            const string characterName = "Human_Temporary(Clone)";
            controller = null;

            while (controller == null)
            {
                yield return null;
                controller = GameObject.Find(characterName)?.GetComponent<HumanoidController>();
            }
        }


        [UnityTest]
        public IEnumerator PlayerMovesCorrectly()
        {
            Vector3 originalPosition;
            Vector3 newPosition;

            // Move West.
            originalPosition = controller.Position;
            yield return Move(HorizontalAxis, -1f );
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(ApproximatelyEqual(newPosition.z, originalPosition.z));

            // Move East.
            originalPosition = controller.Position;
            yield return Move(HorizontalAxis, 1f);
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(ApproximatelyEqual(newPosition.z, originalPosition.z));

            // Move North.
            originalPosition = controller.Position;
            yield return Move(VerticalAxis,-1f);
            newPosition = controller.Position;
            Assert.IsTrue(ApproximatelyEqual(newPosition.x, originalPosition.x));
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);

            // Move South.
            originalPosition = controller.Position;
            yield return Move(VerticalAxis, 1f);
            newPosition = controller.Position;
            Assert.IsTrue(ApproximatelyEqual(newPosition.x, originalPosition.x));
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Northeast.
            originalPosition = controller.Position;
            yield return Move(new[] { VerticalAxis, HorizontalAxis }, new[] { -1f, -1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);

            // Move Southwest.
            originalPosition = controller.Position;
            yield return Move(new[] { VerticalAxis, HorizontalAxis }, new[] { 1f, 1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Northwest.
            originalPosition = controller.Position;
            yield return Move(new[] { VerticalAxis, HorizontalAxis }, new[] { 1f, -1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x < originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z > originalPosition.z);

            // Move Southeast.
            originalPosition = controller.Position;
            yield return Move(new[] { VerticalAxis, HorizontalAxis }, new[] { -1f, 1f });
            newPosition = controller.Position;
            Assert.IsTrue(newPosition.x > originalPosition.x);
            Assert.IsTrue(ApproximatelyEqual(newPosition.y, originalPosition.y));
            Assert.IsTrue(newPosition.z < originalPosition.z);
        }

        private bool ApproximatelyEqual(float a, float b)
        {
            return ((a - b) * (a - b)) < 0.001f;
        }

        public IEnumerator Move(string axis, float value, float duration = 1f)
        {
            yield return Move(new[] {axis }, new [] {value }, duration);
        }

        public IEnumerator Move(string[] axis, float[] value, float duration = 1f)
        {
            // Initial minor delay to enforce separation of commands
            yield return new WaitForSeconds(0.25f);

            // Start holding down the appropriate keys.
            for (int i = 0; i < axis.Length; i++)
            {
                input.SetAxisRaw(axis[i], value[i]);
            }

            // Wait for a little, then release the key.
            yield return new WaitForSeconds(duration);

            // Release the keys.
            for (int i = 0; i < axis.Length; i++)
            {
                input.SetAxisRaw(axis[i], 0);
            }

            // Wait for a little more, to add clear separation from the next move command.
            yield return new WaitForSeconds(0.25f);
        }


        private void PressButton(string buttonName)
        {
            GameObject.Find(buttonName)?.GetComponent<LabelButton>().Press();
        }

        private void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
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
