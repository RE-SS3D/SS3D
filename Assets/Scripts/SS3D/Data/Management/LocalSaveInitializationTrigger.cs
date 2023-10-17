using Coimbra.Services.Events;
using SS3D.Application.Events;
using SS3D.Logging;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Data.Management
{
    /// <summary>
    /// Initializes the local save after the ApplicationInitializing event is triggered.
    /// </summary>
    public class LocalSaveInitializationTrigger : Actor
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Initializing local save files", Logs.Important);

            LocalSave.Initialize();
        }
    }
}