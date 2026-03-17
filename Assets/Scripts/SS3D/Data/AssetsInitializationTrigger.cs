using Coimbra.Services.Events;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using System;

namespace SS3D.Data
{
    /// <summary>
    /// Triggers the initialization of the asset system during application startup.
    /// </summary>
    public class AssetsInitializationTrigger : Actor
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Loading asset databases", Logs.Important);
            InitializeAssetsAsync();
        }

        private async void InitializeAssetsAsync()
        {
            try
            {
                await AssetLoader.InitializeAsync();
            }
            catch (Exception exception)
            {
                Log.Error(this, exception, "AssetLoader initialization failed during application startup.");
            }
        }
    }
}
