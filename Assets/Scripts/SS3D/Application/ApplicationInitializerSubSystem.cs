using Coimbra;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using UDiscord;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConditionIsAlwaysTrueOrFalse
namespace SS3D.Application
{
    /// <summary>
    /// Server as a way to control initialization flux.
    /// </summary>
    public sealed class ApplicationInitializerSubSystem : SubSystem
    {
        protected override void OnStart()
        {
            base.OnStart();

            InitializeApplication();
        }

        /// <summary>
        /// Starts the base systems, loads and caches important data.
        /// </summary>
        public void InitializeApplication()
        {
            Log.Information(this, "Pre initializing application", Logs.Important);
            new ApplicationPreInitializing().Invoke(this);

            Log.Information(this, "Initializing application", Logs.Important);
            new ApplicationInitializing().Invoke(this);

            // TODO: Remove this and the discord integration thing, there should be a better plugin.
            InitializeDiscordIntegration();

            new ApplicationInitialized().Invoke(this);
            Log.Information(this, "Application initialized", Logs.Important);
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
    }
}
