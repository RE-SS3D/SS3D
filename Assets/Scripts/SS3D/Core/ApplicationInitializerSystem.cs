using Coimbra;
using DG.Tweening;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using SS3D.Data;
using SS3D.Logging;
using UDiscord;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Initializes all the core information needed, subsystems and assets pre-loading.
    /// </summary>
    public sealed class ApplicationInitializerSystem : Behaviours.System
    {
        /// <summary>
        /// Initializes all required systems for the application.
        /// </summary>
        public void InitializeApplication()
        {
            Punpun.Information(this, "Initializing application", Logs.Important);

            InitializeSystems();
            InitializeSettings();
            InitializeNetworkSession();
        }

        /// <summary>
        /// Initialize the application settings.
        ///
        /// First it resets the settings if its in the a Built executable.
        /// Then it loads the network settings from the JSON file.
        /// Then it loads the command line args.
        /// </summary>
        private void InitializeSettings()
        {
            if (Application.isEditor)
            {
                return;
            }

            CommandLineArgsSystem startArgsSystem = Subsystems.Get<CommandLineArgsSystem>();

            if (!startArgsSystem.HasCommandLineArgs())
            {
                return;
            }

            NetworkSettings.ResetOnBuiltApplication();
            ApplicationSettings.ResetOnBuiltApplication();

            startArgsSystem.ProcessCommandLineArgs();
        }

        /// <summary>
        /// Initializes the subsystems, like pre-loading assets and integrations.
        /// </summary>
        private void InitializeSystems()
        {
            DOTween.Init();

            InitializeDiscordIntegration();
            InitializeAssetData();
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
