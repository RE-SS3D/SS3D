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

        [Client]
        private void Setup()
        {
            string ckey = LocalPlayerAccountUtility.Ckey;

            bool testingServerOnlyInEditor = IsServer && !IsHost && Application.isEditor;
            if (testingServerOnlyInEditor)
            {
                return;
            }

            UserAuthorizationMessage userAuthorizationMessage = new(ckey);
            InstanceFinder.ClientManager.Broadcast(userAuthorizationMessage);

            CmdRemoveConnectionAfterLogin();
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
            NetworkObject.RemoveOwnership();
        }
    }
}