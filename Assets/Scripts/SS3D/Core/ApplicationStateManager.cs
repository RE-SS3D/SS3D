using System.ComponentModel;
using DG.Tweening;
using SS3D.Core.Networking.Helper;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("_testingSkipIntro")]
        [Header("Test Cases")]
        [SerializeField] private bool _testingSkipIntroInEditor;
        [SerializeField] private bool _testingDisableDiscordIntegrationInEditor;
        [SerializeField] private bool _testingClientInEditor;
        [SerializeField] private bool _testingServerOnlyInEditor;
        
        [Header("Settings Debug")]
        private bool _skipIntro;
        private bool _disableDiscordIntegration;
        private bool _serverOnly;

        public bool ServerOnly => _serverOnly;
        public bool DisableDiscordIntegration => _disableDiscordIntegration;
        public bool SkipIntro => _skipIntro;
        public bool TestingClientInEditor => _testingClientInEditor;
        public bool TestingServerOnlyInEditor => _testingServerOnlyInEditor;

        private void Awake()
        {
            PreProcessTestParams();
            InitializeSingleton();
            InitializeEssentialSystems();
        }

        public void InitializeApplication()
        {
            Debug.Log($"[{nameof(ApplicationStateManager)}] - Initializing application");
            InitializeNetworkSession();
        }

        private void PreProcessTestParams()
        {
            if (Application.isEditor)
            {
                _serverOnly = _testingServerOnlyInEditor;
                _skipIntro = _testingSkipIntroInEditor;
                _disableDiscordIntegration = _testingDisableDiscordIntegrationInEditor;
                
                return;
            }

            _testingSkipIntroInEditor = false;
            _testingDisableDiscordIntegrationInEditor = false;
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

        public void SetServerOnly(bool state)
        {
            _serverOnly = state;
        }
    }
}
