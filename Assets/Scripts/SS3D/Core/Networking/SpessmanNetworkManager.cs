using System;
using System.Collections.Generic;
using Coimbra;
using Mirror;
using SS3D.Core.Networking.Helper;
using SS3D.Core.Networking.PlayerControl.Messages;
using SS3D.Core.Systems.Entities;
using UnityEngine;

namespace SS3D.Core.Networking
{
    /// <summary>
    /// A custom Network Manager to guarantee Mirror won't fuck our game with their base functions
    /// The changes should be minimal in relation to Mirror's
    /// </summary>
    public sealed class SpessmanNetworkManager : NetworkManager
    {
        public static SpessmanNetworkManager Singleton;

        public static event Action OnClientStopped;

        public override void Awake()
        {    
            base.Awake();

            if (Singleton != null) Singleton = this;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            OnClientStopped?.Invoke();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        { 
            Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Client {conn.address} disconnected");

            NetworkIdentity[] ownedObjects = new NetworkIdentity[conn.clientOwnedObjects.Count];
            conn.clientOwnedObjects.CopyTo(ownedObjects);

            if (ownedObjects.Length == 0)
            {
                Debug.LogError($"[{typeof(SpessmanNetworkManager)}] - No clientOwnedObjects were found, something is very wrong");
                return;
            }

            foreach (NetworkIdentity networkIdentity in ownedObjects)
            {
                Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Client {conn.address}'s owned object: {networkIdentity.name}");

                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul == null)
                {
                    Debug.LogError($"[{typeof(SpessmanNetworkManager)}] - No Soul found in clientOwnedObjects, something is very wrong");
                    return;
                }

                NetworkServer.RemovePlayerForConnection(conn, false);
                NetworkServer.SendToAll(new UserLeftServerMessage(soul.Ckey));
                Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Invoking the player server left event: {soul.Ckey}");
            }
        }
    }
}