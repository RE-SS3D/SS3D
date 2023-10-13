using Coimbra;
using DG.Tweening;
using SS3D.Application.Events;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Data.Management;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.SceneManagement;
using System;
using UDiscord;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConditionIsAlwaysTrueOrFalse
namespace SS3D.Application
{
    /// <summary>
    /// Initializes all the core information needed, subsystems and assets pre-loading.
    /// </summary>
    public sealed class ApplicationInitializerSystem : Core.Behaviours.System
    {
        protected override void OnStart()
        {
            base.OnStart();

            StartApplication();
        }

        private void StartApplication()
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            bool isUsingCommandLineArgs = 
#if UNITY_EDITOR
                true;
#else
                Environment.GetCommandLineArgs().Length > 0;
#endif
            Scenes sceneToLoad = isUsingCommandLineArgs ? Scenes.Intro : Scenes.Launcher;

            if (applicationSettings.ForceLauncher || !isUsingCommandLineArgs)
            {
                sceneToLoad = Scenes.Launcher;
            }                                  

            // This call is async and not awaited. Hence the pragma disable.
            #pragma warning disable CS4014
            Scene.LoadAsync(sceneToLoad);
            #pragma warning restore CS4014
        }

        /// <summary>
        /// Initializes all required systems for the application.
        /// </summary>
        public void InitializeApplication()
        {
            Log.Information(this, "Initializing application", Logs.Important);

            DOTween.Init();

			LocalSave.Initialize();

            InitializeDiscordIntegration();
            InitializeAssetData();

            InitializeSettings();

            new ApplicationInitializing().Invoke(this);

            InitializeNetworkSession();

            new ApplicationInitialized().Invoke(this);
        }

        /// <summary>
        /// Initialize the application settings.
        /// </summary>
        private void InitializeSettings()
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            bool isUsingCommandLineArgs =
#if UNITY_EDITOR
                true;
#else
                Environment.GetCommandLineArgs().Length > 0;
#endif

            if (!isUsingCommandLineArgs || applicationSettings.ForceLauncher || UnityEngine.Application.isEditor)
            {
                return;
            }

            NetworkSettings.ResetOnBuiltApplication();
            ApplicationSettings.ResetOnBuiltApplication();
        }

        /// <summary>
        /// Initializes all the assets from the asset data.
        /// </summary>
        private void InitializeAssetData()
        {
            Assets.LoadAssetDatabases();
        }

        /// <summary>
        /// Initializes the Discord integration is enabled on the application systems.
        /// </summary>
        private void InitializeDiscordIntegration()
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            if (applicationSettings.EnableDiscord)
            {
                DiscordManager.Initialize();
            }
            else
            {
                Destroy(FindObjectOfType<DiscordManager>());
            }
        }

        /// <summary>
        /// Initializes the network session, connect, hosting, or starting a server-only application.
        /// </summary>
        private void InitializeNetworkSession()
        {
            Log.Information(this, "Initializing network session", Logs.Important);

            SessionNetworkSystem sessionNetworkSystem = Subsystems.Get<SessionNetworkSystem>();
            sessionNetworkSystem.InitializeNetworkSession();
        }
    }
}
