using FishNet.Object;
using SS3D.Networking.Events;

namespace SS3D.Networking
{
    /// <summary>
    /// Simply calls network connection started events when a network connection is stablished.
    /// </summary>
    public class NetworkConnectionStartedListener : NetworkBehaviour
    { 
        public override void OnStartServer()
        {
            base.OnStartServer();

            new NetworkConnectionStarted().Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            new NetworkConnectionStarted().Invoke(this);
        }
    }
}


