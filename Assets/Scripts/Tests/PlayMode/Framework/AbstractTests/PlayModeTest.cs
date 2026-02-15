using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Coimbra;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using Tests.Play_Mode.Framework.Helpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// All play mode tests should inherit from this class. This class set up the mock up controls, contains some utilities for all play tests.
    /// </summary>
    [TestFixture]
    public abstract class PlayModeTest : InputTestFixture
    {
        protected const string ExecutableName = "SS3D";
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";
        protected const string StartClientCommand = "Start SS3D Server.bat";

        // Use it in [UnitySetUp] method if you want to do special stuff during the first call to such method.
        protected bool setUpOnce = false;

        // Whether the lobby scene has been loaded
        protected bool lobbySceneLoaded = false;

        // Used to simulate mouse input
        protected Mouse mouse;
        protected InputAction leftMouseClick = new InputAction();

        // Used to simulate all possible actions defined in SS3D
        protected InputDevice inputDevice;
        private List<InputAction> inputActions = new();

        public InputDevice InputDevice => inputDevice;

        public Mouse Mouse => mouse;

        protected HumanoidController HumanoidController;
        protected InteractionController InteractionController;

        protected abstract bool UseMockUpInputs();

        /// <summary>
        /// Set up input system and virtual devices for input. Should not contain anything else.
        /// TODO : determine precisely the timing of this method call, as calling it too soon can lead to issues with controllers not set up.
        /// </summary>
        public override void Setup()
        {
            if (UseMockUpInputs())
            {
                UnityEngine.Debug.Log("Calling InputTestFixture.Setup");
                base.Setup();
                // Don't set up a new input device when running multiple tests in a row
                if (inputDevice == null)
                {
                    inputDevice = SetUpMockInputForActions(ref inputActions);
                    InputSystem.AddDevice(inputDevice);
                }
                SetUpMouse();
            }
        }

        /// <summary>
        /// Put input system back to it's original state. Should not contain anything else.
        /// </summary>
        public override void TearDown()
        {
            base.TearDown();
        }

        /// <summary>
        /// TODO : find a better way to set up the mouse device (not with free actions like this).
        /// Only handle left click currently, and only does primary interaction when left clicking.
        /// </summary>
        private void SetUpMouse()
        {
            mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            leftMouseClick.AddBinding(mouse.leftButton);
        }


        public IEnumerator GetHumanoidController(float timeout = 3f)
        {
            float startTime = Time.time;
            HumanoidController = null;
            while (HumanoidController == null)
            {
                yield return null;
                HumanoidController = GameObject.FindWithTag("Player")?.GetComponent<HumanoidController>();
                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Humanoid controller not found within timeout of {timeout} seconds.");
                }
            }
        }

        public IEnumerator GetInteractionController(float timeout = 3f)
        {
            float startTime = Time.time;
            InteractionController = null;
            while (InteractionController == null)
            {
                yield return null;
                InteractionController = TestHelpers.GetLocalInteractionController();
                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Interaction controller not found within timeout of {timeout} seconds.");
                }
            }

            // Set up mouse click so it performs the primary interaction when pressed.
            leftMouseClick.performed += InteractionController.HandleRunPrimary;
            leftMouseClick.Enable();
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
            NetworkSettings originalSettings = ScriptableSettings.GetOrFind<NetworkSettings>();
            NetworkSettings newSettings = UnityEngine.Object.Instantiate(originalSettings);
            newSettings.NetworkType = type;
            newSettings.Ckey = "john";

            // Apply the new settings
            ScriptableSettings.SetOrOverwrite<NetworkSettings>(newSettings);
        }

        protected void ClientSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Game"))
            {
                lobbySceneLoaded = true;
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

        protected void LoadStartupScene()
        {
            // Start up the game.
            lobbySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
        }

        protected void KillAllBuiltExecutables()
        {
            foreach (var process in Process.GetProcessesByName(ExecutableName))
            {
                process.Kill();
            }
        }

        private IEnumerator GetControllers()
        {
            yield return GetHumanoidController();
            yield return GetInteractionController();
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
            InputActionAsset actions = SubSystems.Get<Systems.Inputs.InputSubSystem>().Inputs.asset;
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

        /// <summary>
        /// As host, simply open the game and wait for host to be correctly loaded in lobby.
        /// As client, does the same, but open a dedicated server to connect to before.
        /// </summary>
        /// <param name="type"> The network type we want to load in lobby.</param>
        /// <returns></returns>
        protected IEnumerator LoadAndSetInLobby(NetworkType type)
        {
            if(type is NetworkType.Client)
            {
                LoadFileHelpers.OpenCompiledBuild();

                // Force wait for 10 seconds - this should be long enough for the server to load
                yield return new WaitForSeconds(10f);
                // Set to run as client
                SetApplicationSettings(NetworkType.Client);
            }
            else
            {
                SetApplicationSettings(type);
            }

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();

            yield return WaitForLobbyLoaded();

            // Wait a bit to make sure UI is correctly Loaded
            yield return new WaitForSeconds(3f);
        }

        /// <summary>
        /// This load player (either client or host) in lobby and then in game.
        /// </summary>
        /// <param name="joinDelay"> Time between starting the game and joining it</param>
        /// <returns></returns>
        protected IEnumerator LoadAndSetInGame(NetworkType type, float joinDelay = 0f)
        {
            yield return LoadAndSetInLobby(type);
            yield return SetInGame(joinDelay);
        }

        /// <summary>
        /// Assume the player is in lobby, then launch a round, embark and get the controllers.
        /// </summary>
        /// <param name="joinDelay"> Time between starting the game and joining it.</param>
        /// <returns></returns>
        protected IEnumerator SetInGame(float joinDelay = 0f)
        {
            if (!lobbySceneLoaded)
            {
                throw new Exception("Don't try to get in game without loading the lobby scene first !");
            }
            yield return TestHelpers.StartAndEnterRound(joinDelay);
            yield return GetControllers();
            yield return new WaitForSeconds(1f);
        }
    }
}