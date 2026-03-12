using Coimbra;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Helper for spawning networked objects while ensuring addressable assets
    /// are loaded on all clients before the spawn occurs.
    /// </summary>
    public static class NetworkSpawner
    {
        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance using FishNet.
        /// This is intended for non-addressable prefabs (eg. scene objects or
        /// resources that are guaranteed to be present on all clients).
        ///
        /// This overload does not perform any addressable checks. To ensure an
        /// addressable backing asset is loaded on all clients before spawning,
        /// use the overload that also takes an <see cref="ObjectAssetReference"/>.
        /// </summary>
        public static Task SpawnAsync(NetworkObject networkObject, NetworkConnection ownerConnection = null)
        {
            return SpawnAsync(networkObject, null, ownerConnection);
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its addressable backing asset is loaded on all clients
        /// before the spawn occurs.
        /// </summary>
        /// <param name="networkObject">The already-instantiated network object to spawn.</param>
        /// <param name="assetReference">
        /// Optional addressable asset reference for the prefab this object belongs to.
        /// When provided, the asset will be synchronized and loaded on all clients
        /// before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(NetworkObject networkObject, ObjectAssetReference assetReference, NetworkConnection ownerConnection = null)
        {
            if (!InstanceFinder.IsServer)
            {
                Log.Error(typeof(NetworkSpawner), "NetworkSpawner.Spawn(NetworkObject, ObjectAssetReference) can only be called on the server.");

                return;
            }

            if (!networkObject)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the instance is null.");

                return;
            }

            if (assetReference)
            {
                string databaseId = assetReference.Database;
                string assetId = assetReference.Id;

                if (!await EnsureAddressableLoadedOnAllClientsAsync(databaseId, assetId))
                {
                    // Errors are logged inside EnsureAddressableLoadedOnAllClients.
                    return;
                }
            }

            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

            // Residency is tracked only after a successful spawn so the late-join manifest
            // reflects live world objects rather than attempted spawns.
            TrackActiveAddressableAsset(assetReference, networkObject);
        }

        /// <summary>
        /// Spawns a networked object from an addressable <see cref="ObjectAssetReference"/>.
        ///
        /// Flow:
        /// - Confirms whether the asset is addressable via <see cref="AssetLoader.Has(string, string)"/>.
        /// - If addressable, uses <see cref="AssetSynchronizer"/> to ensure it is
        ///   loaded on all clients before spawning.
        /// - Instantiates the loaded prefab and spawns it using
        ///   <see cref="InstanceFinder.ServerManager.Spawn(NetworkObject, NetworkConnection)"/>.
        /// </summary>
        /// <param name="assetReference">Asset reference for the prefab to spawn.</param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        /// <returns>The spawned <see cref="NetworkObject"/>, or <c>null</c> if spawn failed.</returns>
        public static async Task<NetworkObject> SpawnAsync(ObjectAssetReference assetReference, NetworkConnection ownerConnection = null)
        {
            if (!InstanceFinder.IsServer)
            {
                Log.Error(typeof(NetworkSpawner), "NetworkSpawner.Spawn(ObjectAssetReference) can only be called on the server.");

                return null;
            }

            if (!assetReference)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the asset reference is null.");

                return null;
            }

            string databaseId = assetReference.Database;
            string assetId = assetReference.Id;

            if (!await EnsureAddressableLoadedOnAllClientsAsync(databaseId, assetId))
            {
                // Errors are logged inside EnsureAddressableLoadedOnAllClients.
                return null;
            }

            // Load the prefab locally (this uses the Addressables pipeline when applicable).
            GameObject prefab = await AssetLoader.GetAsync<GameObject>(assetReference);

            if (!prefab)
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to load prefab for asset '{databaseId}/{assetId}'.");

                return null;
            }

            GameObject instance = Object.Instantiate(prefab);

            if (!instance || !instance.TryGetComponent(out NetworkObject networkObject))
            {
                Log.Error(typeof(NetworkSpawner), $"Loaded prefab for asset '{databaseId}/{assetId}' does not contain a NetworkObject component.");
                instance.Dispose(true);

                return null;
            }

            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

            // Residency is tracked only after a successful spawn so the late-join manifest
            // reflects live world objects rather than attempted spawns.
            TrackActiveAddressableAsset(assetReference, networkObject);

            return networkObject;
        }

        /// <summary>
        /// Ensures that an addressable asset identified by database and asset IDs
        /// is loaded on all connected clients. If the asset is not addressable,
        /// this method returns true without performing any work.
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="assetId">The asset identifier within the database.</param>
        /// <returns>
        /// True if the asset is either non-addressable or successfully loaded
        /// on all clients; false if loading failed.
        /// </returns>
        private static async Task<bool> EnsureAddressableLoadedOnAllClientsAsync([CanBeNull] string databaseId, [CanBeNull] string assetId)
        {
            if (string.IsNullOrWhiteSpace(databaseId) || string.IsNullOrWhiteSpace(assetId))
            {
                Log.Error(typeof(NetworkSpawner), "Cannot ensure addressable load because database or asset id is invalid.");

                return false;
            }

            bool isAddressable = AssetLoader.Has(databaseId, assetId);

            // Not addressable, nothing to do.
            if (!isAddressable)
            {
                return true;
            }

            AssetSynchronizer synchronizer = AssetSynchronizer.Instance;

            if (!synchronizer)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot ensure addressable load because AssetSynchronizer instance is missing.");

                return false;
            }

            if (await synchronizer.EnsureLoadedOnAllClientsAsync(databaseId, assetId))
            {
                return true;
            }

            Log.Error(typeof(NetworkSpawner), $"Failed to ensure addressable asset '{databaseId}/{assetId}' is loaded on all clients before spawning.");

            return false;
        }

        /// <summary>
        /// Registers a successfully spawned addressable instance with the active asset registry.
        /// Non-addressable prefabs are ignored because they do not participate in synchronized
        /// load and unload flow for late joiners.
        /// </summary>
        /// <param name="assetReference">Addressable prefab reference associated with the spawned object.</param>
        /// <param name="networkObject">Spawned network object instance to track.</param>
        private static void TrackActiveAddressableAsset([CanBeNull] ObjectAssetReference assetReference, [CanBeNull] NetworkObject networkObject)
        {
            if (!assetReference || !networkObject)
            {
                return;
            }

            string databaseId = assetReference.Database;
            string assetId = assetReference.Id;

            // Only addressable assets are mirrored through the network residency registry.
            if (!AssetLoader.Has(databaseId, assetId))
            {
                return;
            }

            NetworkAssetRegistry.Register(databaseId, assetId, networkObject);
        }
    }
}
