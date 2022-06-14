using FishNet;
using FishNet.Object;
using SS3D.Core.Networking;
using SS3D.Core.Networking.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Core.Systems.Entities
{
    /// <summary>
    /// Class that exists until the player sends auth information
    /// </summary>
    public sealed class UnauthorizedPlayer : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            Setup();
        }

        private void Setup()
        {
            string ckey = LocalPlayerAccountManager.Ckey;

            bool testingServerOnlyInEditor = IsServer && ApplicationStateManager.Instance.TestingServerOnlyInEditor && Application.isEditor;
            if (testingServerOnlyInEditor)
            {
                return;
            }

            CmdRemoveConnectionAfterLogin();
            UserAuthorizationMessage userAuthorizationMessage = new UserAuthorizationMessage(ckey);
            InstanceFinder.ClientManager.Broadcast(userAuthorizationMessage);

            CmdDestroyObjectAfterLogin();
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdDestroyObjectAfterLogin()
        {
            ServerManager.Despawn(gameObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdRemoveConnectionAfterLogin()
        {
            string ckey = LocalPlayerAccountManager.Ckey;
    
            Debug.Log($"[{typeof(UnauthorizedPlayer)}] - OnStartLocalPlayer - Destroying temporary player for {ckey}");
            
            NetworkObject.RemoveOwnership();
        }
    }
}