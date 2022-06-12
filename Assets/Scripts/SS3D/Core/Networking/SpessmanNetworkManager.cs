using System.Collections.Generic;
using System.Security.Cryptography;
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
        public static SpessmanNetworkManager Instance;

        public override void Awake()
        {
            base.Awake();

            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            Debug.Log($"[{nameof(SpessmanNetworkManager)}] - Client {conn.address} disconnected");

            NetworkIdentity[] ownedObjects = new NetworkIdentity[conn.clientOwnedObjects.Count];
            conn.clientOwnedObjects.CopyTo(ownedObjects);

            if (ownedObjects.Length == 0)
            {
                Debug.LogError($"[{nameof(SpessmanNetworkManager)}] - No clientOwnedObjects were found, something is very wrong");
                return;
            }

            foreach (NetworkIdentity networkIdentity in ownedObjects)
            {
                Debug.Log($"[{nameof(SpessmanNetworkManager)}] - Client {conn.address}'s owned object: {networkIdentity.name}");

                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul == null)
                {
                    Debug.LogError($"[{nameof(SpessmanNetworkManager)}] - No Soul found in clientOwnedObjects, something is very wrong");
                    return;
                }

                NetworkServer.RemovePlayerForConnection(conn, false);
                NetworkServer.SendToAll(new UserLeftServerMessage(soul.Ckey));
                Debug.Log($"[{nameof(SpessmanNetworkManager)}] - Invoking the player server left event: {soul.Ckey}");
            }
        }
    }
}