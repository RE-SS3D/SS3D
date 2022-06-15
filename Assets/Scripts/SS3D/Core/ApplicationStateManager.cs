using System;
using DG.Tweening;
using SS3D.Core.Networking;
using SS3D.Core.Networking.Helper;
using SS3D.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Core
{
    /// <summary>
    /// Responsible for controlling the game state, persistent throughout the application
    /// Should hopefully be the only Singleton in the project
    /// </summary>
    public sealed class ApplicationStateManager : MonoBehaviour
    {
        public static ApplicationStateManager Instance;

        [Header("Managers")]
        [SerializeField] private SessionNetworkHelper _networkHelper;

        [Header("Settings")] 
        [SerializeField] private bool _skipIntro;
        [SerializeField] private bool _disableDiscordIntegration;

        [Header("Test Cases")]
        [SerializeField] private bool _testingClientInEditor;
        [SerializeField] private bool _testingServerOnlyInEditor;

        public bool DisableDiscordIntegration => _disableDiscordIntegration;
        public bool SkipIntro => _skipIntro;
        public bool TestingClientInEditor => _testingClientInEditor;
        public bool TestingServerOnlyInEditor => _testingServerOnlyInEditor;

        private void Awake()
        {
            InitializeSingleton();
            InitializeEssentialSystems();
        }

        public void InitializeApplication()
        {
            Debug.Log($"[{nameof(ApplicationStateManager)}] - Initializing application");

            ProcessTestParams();
            InitializeNetworkSession();
        }

        private void ProcessTestParams()
        {
            if (Application.isEditor)
            {
                return;
            }

            _testingClientInEditor = false;
            _testingServerOnlyInEditor = false;
        }

        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            Debug.Log($"[{nameof(ApplicationStateManager)}] - Initializing Application State Manager singleton");
        }

        private void InitializeEssentialSystems()
        {
            DOTween.Init();
            Debug.Log($"[{nameof(ApplicationStateManager)}] - Initializing essential systems");
        }

        private void InitializeNetworkSession() 
        {
            Debug.Log($"[{nameof(ApplicationStateManager)}] - Initializing network session");
            _networkHelper.InitiateNetworkSession();
        }

        public void SetSkipIntro(bool state)
        {
            _skipIntro = state;
        }

        public void SetDisableDiscordIntegration(bool state)
        {
            _disableDiscordIntegration = state;
        }
    }
}
