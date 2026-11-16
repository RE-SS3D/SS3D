using Coimbra.Services.Events;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using UnityEngine;
using UnityApplication = UnityEngine.Application;

namespace SS3D.Data
{
    /// <summary>
    /// Triggers the initialization of the AssetData system.
    /// </summary>
	public class AssetsInitializationTrigger : Actor
	{
		protected override void OnAwake()
		{
			base.OnAwake();

			AddHandle(ApplicationInitializing.AddListener(HandleApplicationInitializing));
            UnityApplication.quitting += HandleApplicationQuitting;
		}

        protected override void OnDestroyed()
        {
            UnityApplication.quitting -= HandleApplicationQuitting;
            base.OnDestroyed();
        }

		private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Debug(this, "Loading asset databases", Logs.Important);
            
			Assets.LoadAssetDatabases();
            Assets.PreloadAddressableDatabases();
		}

        private static void HandleApplicationQuitting()
        {
            AssetProvider.ReleaseAll();
        }
	}
}
