using Coimbra;
using DG.Tweening;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.SceneManagement;
using UDiscord;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Initializes all the core information needed, subsystems and assets pre-loading.
    /// </summary>
    public sealed class ApplicationInitializerSystem : Behaviours.System
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            InitializeLauncher();
        }

        private void InitializeLauncher()
        {
            CommandLineArgsSystem startArgsSystem = Subsystems.Get<CommandLineArgsSystem>();
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            Scenes sceneToLoad = startArgsSystem.HasCommandLineArgs() ? Scenes.Intro : Scenes.Launcher;

            if (applicationSettings.ForceLauncher && !startArgsSystem.HasCommandLineArgs())
            {
                sceneToLoad = Scenes.Launcher;
            }                                  

            Scene.LoadAsync(sceneToLoad);
        }

        /// <summary>
        /// Initializes all required systems for the application.
        /// </summary>
        public void InitializeApplication()
        {
            Punpun.Information(this, "Initializing application", Logs.Important);

            DOTween.Init();

            InitializeDiscordIntegration();
            InitializeAssetData();

            InitializeSettings();
            InitializeNetworkSession();
        }

        /// <summary>
        /// Initialize the application settings.
        /// </summary>
        private void InitializeSettings()
        {
            CommandLineArgsSystem startArgsSystem = Subsystems.Get<CommandLineArgsSystem>();
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            if (!startArgsSystem.HasCommandLineArgs() ||applicationSettings.ForceLauncher || Application.isEditor)
            {
                return;
            }

            NetworkSettings.ResetOnBuiltApplication();
            ApplicationSettings.ResetOnBuiltApplication();

            startArgsSystem.ProcessCommandLineArgs();
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
            Punpun.Information(this, "Initializing network session", Logs.Important);

            SessionNetworkSystem sessionNetworkSystem = Subsystems.Get<SessionNetworkSystem>();
            sessionNetworkSystem.InitializeNetworkSession();
        }
    }
}
