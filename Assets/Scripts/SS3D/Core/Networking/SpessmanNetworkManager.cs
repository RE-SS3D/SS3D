using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using SS3D.Core.Networking.PlayerControl.Messages;
using SS3D.Core.Systems.Entities;
using UnityEngine;

namespace SS3D.Core.Networking
{
    /// <summary>
    /// A custom Network Manager to guarantee Mirror won't fuck our game with their base functions
    /// The changes should be minimal in relation to Mirror's
    /// </summary>
    public sealed class SpessmanNetworkManager : MonoBehaviour
    {
        private void Start()
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnServerDisconnect;
        }

        private void OnServerDisconnect(NetworkConnection conn, RemoteConnectionStateArgs remoteConnectionStateArgs)
        { 
            Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Client {conn.GetAddress()} disconnected");

            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            

            if (ownedObjects.Length == 0)
            {
                Debug.LogError($"[{typeof(SpessmanNetworkManager)}] - No clientOwnedObjects were found, something is very wrong");
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Client {conn.GetAddress()}'s owned object: {networkIdentity.name}");

                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul == null)
                {
                    Debug.LogError($"[{typeof(SpessmanNetworkManager)}] - No Soul found in clientOwnedObjects, something is very wrong");
                    return;
                }

                networkIdentity.RemoveOwnership();
                InstanceFinder.ServerManager.Broadcast(new UserLeftServerMessage(soul.Ckey));
                Debug.Log($"[{typeof(SpessmanNetworkManager)}] - Invoking the player server left event: {soul.Ckey}");
            }
        }
    }
}