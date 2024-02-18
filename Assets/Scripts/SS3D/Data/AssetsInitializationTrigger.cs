using Coimbra.Services.Events;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Logging;

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

			ApplicationInitializing.AddListener(HandleApplicationInitializing);
		}

		private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Loading asset databases", Logs.Important);
            
			Assets.LoadAssetDatabases();
		}
	}
}