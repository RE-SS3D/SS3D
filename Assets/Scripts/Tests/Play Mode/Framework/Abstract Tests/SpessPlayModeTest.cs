using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Coimbra;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [TestFixture]
    public abstract class SpessPlayModeTest : InputTestFixture
    {
        protected const string ExecutableName = "SS3D";

        // Whether the lobby scene has been loaded
        protected bool lobbySceneLoaded = false;
        protected bool emptySceneLoaded = false;

        // Used to simulate input
        //protected Keyboard keyboard;
        protected Mouse mouse;
        protected InputDevice inputDevice;
        private List<InputAction> inputActions = new();

        public InputDevice InputDevice => inputDevice;

        public override void Setup()
        {
            UnityEngine.Debug.Log("Calling InputTestFixture.Setup");
            base.Setup();
            //mouse = UnityEngine.InputSystem.InputSystem.AddDevice<Mouse>();
            inputDevice = SetUpMockInputForActions(ref inputActions);
            UnityEngine.InputSystem.InputSystem.AddDevice(inputDevice);
            
            mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
        }
        public void LeftMouseClicked(InputAction.CallbackContext context)
        {
            UnityEngine.Debug.Log("LeftMouseClicked");
        }
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnitySetUp]
        public virtual IEnumerator UnitySetUp()
        {
            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return new WaitForSeconds(1f);
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            // Wait for a bit, to get some temporal separation.
            yield return new WaitForSeconds(1f);
        }

        protected InputAction GetAction(string name)
        {
            foreach (InputAction action in inputActions)
            {
                UnityEngine.Debug.Log(action.name);
                if (action.name == name)
                {
                    return action;
                }
            }
            UnityEngine.Debug.Log($"ERROR! No action of name {name} found!");
            return null;
        }

        protected void SetApplicationSettings(NetworkType type)
        {
            // Create new settings so that tests are run in the correct context.
            ApplicationSettings originalSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
            ApplicationSettings newSettings = ScriptableObject.Instantiate(originalSettings);
            newSettings.NetworkType = type;
            newSettings.Ckey = "john";

            // Apply the new settings
            ScriptableSettings.SetOrOverwrite<ApplicationSettings>(newSettings);
        }

        protected void ClientSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Lobby"))
            {
                lobbySceneLoaded = true;
                SceneManager.sceneLoaded -= ClientSceneLoaded;
            }

            if (scene.name.Equals("Empty"))
            {
                emptySceneLoaded = true;
                SceneManager.sceneLoaded -= ClientSceneLoaded;
            }
        }

        protected IEnumerator WaitForLobbyLoaded(float timeout = 20f)
        {
            float startTime = Time.time;
            while (!lobbySceneLoaded)
            {
                yield return new WaitForSeconds(1f);

                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Lobby not loaded within timeout of {timeout} seconds.");
                }
            }
        }

        protected IEnumerator WaitForEmptySceneLoaded(float timeout = 10f)
        {
            float startTime = Time.time;
            while (!emptySceneLoaded)
            {
                yield return new WaitForSeconds(1f);

                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Empty scene not loaded within timeout of {timeout} seconds.");
                }
            }
        }

        protected void LoadStartupScene()
        {
            // Start up the game.
            lobbySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Startup", LoadSceneMode.Single);
        }

        protected void LoadEmptyScene()
        {
            // Close down the game.
            emptySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Empty", LoadSceneMode.Single);
        }

        protected void KillAllBuiltExecutables()
        {
            foreach (var process in Process.GetProcessesByName(ExecutableName))
            {
                process.Kill();
            }
        }

        protected void EnableInput()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {

            }

        }


        /// <summary>
        /// A simple means of running a UnityTest multiple times to see if it consistently works.
        /// To use this, simply paste "[ValueSource("Iterations")] int iteration" as the argument
        /// to the UnityTest you want to repeat.
        /// </summary>
        /// <returns>An array of the size specified by the Repetition constant.</returns>
        protected static int[] Iterations()
        {
            const int Repetitions = 10;

            int[] result = new int[Repetitions];
            for (int i = 0; i < Repetitions; i++)
            {
                result[i] = i;
            }
            return result;
        }

        /// <summary>
        /// Take a set of actions and create an InputDevice for it that has a control for each
        /// of the actions. Also binds the actions to that those controls. Gratefully adapted from
        /// https://rene-damm.github.io/HowDoI.html#set-an-actions-value-programmatically
        /// </summary>
        /// <returns>A mock device for use in testing.</returns>
        public static InputDevice SetUpMockInputForActions(ref List<InputAction> inputActions)
        {
            UnityEngine.Debug.Log("Entering SetUpMockInput");
            InputActionAsset actions = Subsystems.Get<Systems.Inputs.InputSystem>().Inputs.asset;
            UnityEngine.Debug.Log(actions.ToString());

            var layoutName = actions.name;

            // Build a device layout that simply has one control for each action in the asset.
            InputSystem.RegisterLayoutBuilder(() =>
            {
                var builder = new InputControlLayout.Builder()
                    .WithName(layoutName);

                foreach (var action in actions)
                {
                    builder.AddControl(action.name) // Must not have actions in separate maps with the same name.
                        .WithLayout(action.expectedControlType);
                }

                return builder.Build();
            }, name: layoutName);

            // Create the device.
            var device = InputSystem.AddDevice(layoutName);
            UnityEngine.Debug.Log(device.ToString());


            // Add a control scheme for it to the actions.
            actions.AddControlScheme("MockInput")
                .WithRequiredDevice($"<{layoutName}>");

            // Bind the actions in the newly added control scheme.
            foreach (var action in actions)
            {
                inputActions.Add(action);
                action.AddBinding($"<{layoutName}>/{action.name}", groups: "MockInput");
                UnityEngine.Debug.Log($"Added binding <{layoutName}>/{action.name}");
            }


            // Restrict the actions to bind only to our newly created
            // device using the bindings we just added.
            //actions.bindingMask = InputBinding.MaskByGroup("MockInput");
            actions.devices = new[] { device };

            UnityEngine.Debug.Log("Returning device.");

            return device;
        }
    }
}