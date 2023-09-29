using Coimbra;
using FishNet.Object;
using SS3D.Core.Settings;
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

        [Client(RequireOwnership = true)]
        private void Setup()
        {
            string ckey = LocalPlayer.Ckey;

            bool testingServerOnlyInEditor = IsServer && !IsHost && Application.isEditor;
            if (testingServerOnlyInEditor)
            {
                return;
            }

            Log.Information(this, "Attempting authentication for user {ckey}", Logs.ClientOnly, ckey);

            UserAuthorizationMessage userAuthorizationMessage = new(ckey);
            ClientManager.Broadcast(userAuthorizationMessage);

            CmdDestroyObjectAfterLogin();
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdDestroyObjectAfterLogin()
        {
            ServerManager.Despawn(gameObject);
        }
    }
}