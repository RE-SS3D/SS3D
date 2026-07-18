using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet;
using FishNet.Managing;
using NUnit.Framework;
using SS3D.Application;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SS3D.Tests
{
    /// <summary>
    /// All play mode tests should inherit from this class. This class set up the mock up controls, contains some utilities for all play tests.
    /// Single-process (Host or in-Editor DedicatedServer) only - for real multi-process
    /// coverage, see Testing/multiplayer/ (Documents/architecture/2026-07_multiplayer-test-harness.md).
    /// </summary>
    [TestFixture]
    public abstract class PlayModeTest : InputTestFixture
    {
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";

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

        private static NetworkSettings _baselineNetworkSettings;
        private static ApplicationSettings _baselineApplicationSettings;

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
            NetworkSettings baselineNetworkSettings = GetBaselineNetworkSettings();
            ApplicationSettings baselineApplicationSettings = GetBaselineApplicationSettings();

            NetworkSettings networkSettings = UnityEngine.Object.Instantiate(baselineNetworkSettings);
            networkSettings.NetworkType = type;
            networkSettings.Ckey = "john";
            networkSettings.ServerAddress = "127.0.0.1";

            ScriptableSettings.SetOrOverwrite(networkSettings);

            ApplicationSettings applicationSettings = UnityEngine.Object.Instantiate(baselineApplicationSettings);
            applicationSettings.SkipIntro = true;
            applicationSettings.EnableDiscord = false;
            applicationSettings.ForceLauncher = false;
            ScriptableSettings.SetOrOverwrite(applicationSettings);
        }

        private static NetworkSettings GetBaselineNetworkSettings()
        {
            if (_baselineNetworkSettings != null)
            {
                return _baselineNetworkSettings;
            }

#if UNITY_EDITOR
            _baselineNetworkSettings = AssetDatabase.LoadAssetAtPath<NetworkSettings>("Assets/Settings/NetworkSettings.asset");
#endif
            if (_baselineNetworkSettings == null)
            {
                _baselineNetworkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();
            }

            return _baselineNetworkSettings;
        }

        private static ApplicationSettings GetBaselineApplicationSettings()
        {
            if (_baselineApplicationSettings != null)
            {
                return _baselineApplicationSettings;
            }

#if UNITY_EDITOR
            _baselineApplicationSettings = AssetDatabase.LoadAssetAtPath<ApplicationSettings>("Assets/Settings/ApplicationSettings.asset");
#endif
            if (_baselineApplicationSettings == null)
            {
                _baselineApplicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
            }

            return _baselineApplicationSettings;
        }

        protected IEnumerator PrepareNetworkTestEnvironment()
        {
            CloseActiveNetworkSession();
            lobbySceneLoaded = false;
            setUpOnce = false;
            yield return null;
            yield return new WaitForSeconds(1f);
        }

        protected void CloseActiveNetworkSession()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                return;
            }

            if (networkManager.ServerManager != null && networkManager.ServerManager.Started)
            {
                networkManager.ServerManager.StopConnection(true);
            }

            if (networkManager.ClientManager != null && networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StopConnection();
            }
        }

        protected void ClientSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Game"))
            {
                lobbySceneLoaded = true;
                SceneManager.sceneLoaded -= ClientSceneLoaded;
            }
        }

        protected IEnumerator WaitForLobbyLoaded(float timeout = 60f)
        {
            float startTime = Time.time;

            while (!IsLobbySceneActive())
            {
                yield return new WaitForSeconds(0.5f);

                if (Time.time - startTime > timeout)
                {
                    throw new Exception(BuildLobbyTimeoutMessage(timeout));
                }
            }

            lobbySceneLoaded = true;
        }

        private static bool IsLobbySceneActive()
        {
            return SceneManager.GetActiveScene().name == "Game";
        }

        private static string BuildLobbyTimeoutMessage(float timeout)
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            string networkState = networkManager == null
                ? "NetworkManager missing"
                : $"clientStarted={networkManager.ClientManager.Started}, serverStarted={networkManager.ServerManager.Started}";

            return
                $"Lobby (Game scene) not loaded within {timeout} seconds. " +
                $"Active scene: {SceneManager.GetActiveScene().name}. {networkState}. " +
                "Boot flow is Boot -> Intro -> network session -> Game. Check for stale NetworkManager instances or port conflicts.";
        }

        protected void LoadStartupScene()
        {
            // Start up the game.
            lobbySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
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
        /// Opens the game in this process (Host, or an in-Editor DedicatedServer) and waits
        /// for it to be correctly loaded in lobby. Does not support NetworkType.Client - a
        /// real client is a separate process, which this single-process fixture cannot drive.
        /// See Testing/multiplayer/ for real client&lt;-&gt;server coverage.
        /// </summary>
        /// <param name="type"> The network type we want to load in lobby.</param>
        /// <returns></returns>
        protected IEnumerator LoadAndSetInLobby(NetworkType type)
        {
            yield return PrepareNetworkTestEnvironment();

            SetApplicationSettings(type);

            LoadStartupScene();

            yield return WaitForLobbyLoaded();

            yield return new WaitForSeconds(1f);
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