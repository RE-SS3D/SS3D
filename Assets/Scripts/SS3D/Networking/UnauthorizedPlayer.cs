using FishNet;
using FishNet.Object;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using SS3D.Logging;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Networking
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
            string ckey = LocalPlayer.Ckey;

            bool testingServerOnlyInEditor = IsServer && !IsHost && Application.isEditor;
            if (testingServerOnlyInEditor)
            {
                return;
            }

            Punpun.Say(this, $"Attempting authentication for user {ckey}", Logs.ClientOnly);

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