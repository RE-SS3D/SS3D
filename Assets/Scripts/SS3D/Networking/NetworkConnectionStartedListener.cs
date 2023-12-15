using Cysharp.Threading.Tasks;
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

            TriggerStartServer().Forget();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            TriggerStartClient().Forget();
        }

        private async UniTask TriggerStartServer()
        {
            await UniTask.DelayFrame(1);

            new NetworkConnectionStarted().Invoke(this);
        }

        private async UniTask TriggerStartClient()
        {
            await UniTask.DelayFrame(1);

            new NetworkConnectionStarted().Invoke(this);
        }
    }
}


