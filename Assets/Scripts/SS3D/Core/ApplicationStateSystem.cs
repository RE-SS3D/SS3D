using Coimbra;
using DG.Tweening;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using SS3D.Data;
using SS3D.Logging;
using UDiscord;

namespace SS3D.Core
{
    /// <summary>
    /// Initializes all the core information needed, subsystems and assets pre-loading.
    /// </summary>
    public sealed class ApplicationStateSystem : Behaviours.System
    {
        /// <summary>
        /// Initializes all required systems for the application.
        /// </summary>
        public void InitializeApplication()
        {
            Punpun.Information(this, "Initializing application", Logs.Important);

            InitializeSubsystems();
            InitializeApplicationSettings();
            InitializeNetworkSession();
        }

        /// <summary>
        /// Initializes the application settings based on the command line args or the Project Settings if in Editor.
        /// </summary>
        private void InitializeApplicationSettings()
        {
            CommandLineArgsSystem startArgsSystem = Subsystems.Get<CommandLineArgsSystem>();

            startArgsSystem.LoadApplicationSettings();
            startArgsSystem.ProcessCommandLineArgs();
        }

        /// <summary>
        /// Initializes the subsystems, like pre-loading assets and integrations.
        /// </summary>
        private void InitializeSubsystems()
        {
            DOTween.Init();

            InitializeDiscordIntegration();
            InitializeAssetData();
        }

        /// <summary>
        /// Initializes all the assets from the AssetData.
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
            bool enableDiscordIntegration = applicationSettings.EnableDiscord;

            if (enableDiscordIntegration)
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
