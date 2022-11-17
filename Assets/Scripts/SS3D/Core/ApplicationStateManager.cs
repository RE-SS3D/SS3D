using DG.Tweening;
using SS3D.Data;
using SS3D.Logging;
using UDiscord;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Responsible for controlling the game state, persistent throughout the application
    /// Should hopefully be the only Singleton in the project
    /// </summary>
    public sealed class ApplicationStateManager : MonoBehaviour
    {
        public static ApplicationStateManager Instance;

        [Header("Test Cases")]
        [SerializeField] private bool _testingSkipIntroInEditor;
        [SerializeField] private bool _testingDiscordIntegrationInEditor;
        [SerializeField] private bool _testingClientInEditor;
        [SerializeField] private bool _testingServerInEditor;

        public static bool IsServer { get; private set; }
        public static bool IsClient { get; private set; }
        public static bool EnableDiscordIntegration { get; private set; }
        public static bool SkipIntro { get; private set; }

        private void Awake()
        {
            PreProcessTestParams();
            InitializeSingleton();
            InitializeEssentialSystems();   
            InitializeSubsystems();
        }

        private void InitializeSubsystems()
        {
            DiscordManager.Initialize();
            AssetData.InitializeDatabases();
        }

        public void InitializeApplication()
        {
            Punpun.Say(typeof(ApplicationStateManager), "Initializing SS3D application");
            InitializeNetworkSession();
        }

        private void PreProcessTestParams()
        {
            if (Application.isEditor)
            {
                IsClient = _testingClientInEditor;
                IsServer = _testingServerInEditor;
                SkipIntro = _testingSkipIntroInEditor;
                EnableDiscordIntegration = _testingDiscordIntegrationInEditor;

                return;
            }

            _testingServerInEditor = false;
            _testingSkipIntroInEditor = false;
            _testingClientInEditor = false;
            _testingDiscordIntegrationInEditor = false;
        }

        private void InitializeSingleton()
        {
            Punpun.Say(typeof(ApplicationStateManager), "Initializing Application State Manager singleton");

            if (Instance == null)
            {
                Instance = this;
            }
        }

        private static void InitializeEssentialSystems()
        {
            Punpun.Say(typeof(ApplicationStateManager), "Initializing essential systems");
            DOTween.Init();
        }

        private static void InitializeNetworkSession() 
        {
            Punpun.Say(typeof(ApplicationStateManager), "Initializing network session");
            SessionNetworkHelper.InitiateNetworkSession();
        }

        public static void SetSkipIntro(bool state)
        {
            SkipIntro = state;
        }

        public static void SetDiscordIntegration(bool state)
        {
            EnableDiscordIntegration = state;
        }

        public static void SetServerOnly(bool state)
        {
            IsServer = state;
            IsClient = !state;
        }
    }
}
