using FishNet.Object;
using System;

namespace SS3D.Core.Events
{
    /// <summary>
    /// Simple class used to trigger a call once client and/or server is fully functionnal and started.
    /// Useful for static classes.
    /// </summary>
    public class ServerStartedListener : NetworkBehaviour
    {
        public event EventHandler OnServerOrClientStarted;

        public void Awake()
        {
            OnServerOrClientStarted += LogManager.OnServerStarted;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            OnServerOrClientStarted?.Invoke(this,EventArgs.Empty);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnServerOrClientStarted?.Invoke(this, EventArgs.Empty);
        }
    }
}


