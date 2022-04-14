using System;
using Coimbra;
using Mirror;
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
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Setup();
        }

        private void Setup()
        {
            string ckey = LocalPlayerAccountManager.Ckey;

            bool testingServerOnlyInEditor = isServer && ApplicationStateManager.Instance.TestingServerOnlyInEditor && Application.isEditor;
            if (testingServerOnlyInEditor)
            {
                return;
            }

            CmdRemoveConnectionAfterLogin();
            UserAuthorizationMessage userAuthorizationMessage = new UserAuthorizationMessage(ckey);
            NetworkClient.Send(userAuthorizationMessage);

            CmdDestroyObjectAfterLogin();
        }

        [Command(requiresAuthority = false)]
        private void CmdDestroyObjectAfterLogin()
        {
            NetworkServer.Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        private void CmdRemoveConnectionAfterLogin()
        {
            string ckey = LocalPlayerAccountManager.Ckey;
    
            Debug.Log($"[{typeof(UnauthorizedPlayer)}] - OnStartLocalPlayer - Destroying temporary player for {ckey}");
            NetworkServer.RemovePlayerForConnection(connectionToClient, false);
        }
    }
}