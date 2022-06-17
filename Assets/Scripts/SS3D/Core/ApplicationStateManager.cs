using DG.Tweening;
using SS3D.Core.Networking.Helper;
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
        [SerializeField] private bool _testingServerInEditor;

        public static bool IsServer { get; private set; }
        public static bool IsClient { get; private set; }
        public static bool DisableDiscordIntegration { get; private set; }
        public static bool SkipIntro { get; private set; }

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
                IsClient = _testingClientInEditor;
                IsServer = _testingServerInEditor;
                SkipIntro = _testingSkipIntroInEditor;
                DisableDiscordIntegration = _testingDisableDiscordIntegrationInEditor;

                return;
            }

            _testingServerInEditor = false;
            _testingSkipIntroInEditor = false;
            _testingClientInEditor = false;
            _testingDisableDiscordIntegrationInEditor = false;
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

        public static void SetSkipIntro(bool state)
        {
            SkipIntro = state;
        }

        public static void SetDisableDiscordIntegration(bool state)
        {
            DisableDiscordIntegration = state;
        }

        public static void SetServerOnly(bool state)
        {
            IsServer = state;
            IsClient = !state;
        }
    }
}
