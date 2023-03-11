using System.Collections;
using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Tests
{
    public abstract class SpessHostPlayModeTest : SpessPlayModeTest
    {
        protected HumanoidController controller;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {

            // Set to run as host
            SetApplicationSettings(NetworkType.Host);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {

        }

        protected IEnumerator StartAndEnterRound()
        {
            yield return TestHelpers.StartAndEnterRound();
            yield return GetController();
        }

        protected IEnumerator FinishAndExitRound()
        {
            yield return TestHelpers.FinishAndExitRound();

        }

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();

            // TODO: Create our fake input and assign it to the player

            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return new WaitForSeconds(1f);
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            // Wait for a bit, to get some temporal separation.
            yield return new WaitForSeconds(1f);

            yield return base.UnityTearDown();
        }

        public IEnumerator GetController()
        {
            const string characterName = "HumanTemporary(Clone)";
            controller = null;

            while (controller == null)
            {
                yield return null;
                controller = GameObject.Find(characterName)?.GetComponent<HumanoidController>();
            }
        }

        public bool ApproximatelyEqual(float a, float b)
        {
            bool result = ((a - b) * (a - b)) < 0.001f;
            return result;
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
                // TODO: Set up fake input
                //input.SetAxisRaw(axis[i], value[i]);
            }

            // Wait for a little, then release the key.
            yield return new WaitForSeconds(duration);

            // Release the keys.
            for (int i = 0; i < axis.Length; i++)
            {
                //input.SetAxisRaw(axis[i], 0);
            }

            // Wait for a little more, to add clear separation from the next move command.
            yield return new WaitForSeconds(0.25f);
        }


        public void PressButton(string buttonName)
        {
            GameObject.Find(buttonName)?.GetComponent<LabelButton>().Press();
        }

        public void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
        }
    }
}
