using System.Linq;
using FishNet;
using FishNet.Connection;
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
    public sealed class PlayerConnectionSystem : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _userPrefab;

        private void Start()
        {
            AddEventListeners();
        }

        private void AddEventListeners()
        {
            InstanceFinder.SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            InstanceFinder.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs remoteConnectionStateArgs)
        {
            if (remoteConnectionStateArgs.ConnectionState == RemoteConnectionState.Stopped)
            {
                ProcessPlayerDisconnect(conn);
            }
        }

        private void HandleClientLoadedStartScenes(NetworkConnection networkConnection, bool asServer)
        {
            ProcessPlayerJoin(networkConnection);
        }

        private void ProcessPlayerJoin(NetworkConnection conn)
        {
            Debug.Log($"[{nameof(PlayerConnectionSystem)}] - Player joined the server - {conn.ClientId} {conn.GetAddress()}");
            
            NetworkObject soul = Instantiate(_userPrefab, Vector3.zero, Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(soul, conn);
        }
        
        private void ProcessPlayerDisconnect(NetworkConnection conn)
        {
            Debug.Log($"[{nameof(PlayerConnectionSystem)}] - Client {conn.ClientId} {conn.GetAddress()} disconnected");
            
            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            if (ownedObjects.Length == 0)
            {
                Debug.LogError($"[{nameof(PlayerConnectionSystem)}] - No clientOwnedObjects were found, something is very wrong");
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Debug.Log($"[{nameof(PlayerConnectionSystem)}] - Client {conn.GetAddress()}'s owned object: {networkIdentity.name}");
    
                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul == null)
                {
                    Debug.LogError($"[{nameof(PlayerConnectionSystem)}] - No Soul found in clientOwnedObjects, something is very wrong");
                    return;
                }

                networkIdentity.RemoveOwnership();
                InstanceFinder.ServerManager.Broadcast(new UserLeftServerMessage(soul.Ckey));
                Debug.Log($"[{nameof(PlayerConnectionSystem)}] - Invoking the player server left event: {soul.Ckey}");
            }
        }
    }
}