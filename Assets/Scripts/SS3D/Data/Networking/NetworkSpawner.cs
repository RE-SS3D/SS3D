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
                AssetKey assetKey = new(assetReference.Database, assetReference.Id);

                if (!await EnsureAddressableLoadedOnAllClientsAsync(assetKey))
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
        /// - Confirms whether the asset can be resolved through the async runtime-loading path via <see cref="AssetSubSystem.Has(string,string)"/>.
        /// - Uses <see cref="AssetSynchronizer"/> to ensure it is loaded on all clients before spawning.
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

            AssetKey assetKey = new(assetReference.Database, assetReference.Id);

            if (!await EnsureAddressableLoadedOnAllClientsAsync(assetKey))
            {
                // Errors are logged inside EnsureAddressableLoadedOnAllClients.
                return null;
            }

            // Load the prefab locally using the shared network ownership claim so the synchronized
            // preload and the spawned live world object contribute to the same loader residency.
            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to load prefab for asset '{assetKey}' because {nameof(AssetSubSystem)} instance is missing.");

                return null;
            }

            GameObject prefab = await assetSubSystem.AcquireNetworkAssetAsync<GameObject>(assetKey);

            if (!prefab)
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to load prefab for asset '{assetKey}'.");
                ReleaseFailedAddressableSpawn(assetKey);

                return null;
            }

            GameObject instance = Object.Instantiate(prefab);

            if (!instance || !instance.TryGetComponent(out NetworkObject networkObject))
            {
                Log.Error(typeof(NetworkSpawner), $"Loaded prefab for asset '{assetKey}' does not contain a NetworkObject component.");
                instance.Dispose(true);
                ReleaseFailedAddressableSpawn(assetKey);

                return null;
            }

            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

            // Residency is tracked only after a successful spawn so the late-join manifest
            // reflects live world objects rather than attempted spawns.
            TrackActiveAddressableAsset(assetReference, networkObject);

            return networkObject;
        }

        /// <summary>
        /// Ensures that an async-loadable asset identified by database and asset IDs
        /// is loaded on all connected clients before the spawn proceeds.
        /// Legacy direct-reference assets should use the overload that spawns an existing instance instead of entering this synchronized path.
        /// </summary>
        /// <returns>
        /// True if the asset was successfully loaded on all clients; false if the
        /// asset is invalid for the synchronized path or loading failed.
        /// </returns>
        private static async Task<bool> EnsureAddressableLoadedOnAllClientsAsync(AssetKey assetKey)
        {
            if (!assetKey.IsValid)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot ensure addressable load because database or asset id is invalid.");

                return false;
            }

            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot ensure addressable load for '{assetKey}' because {nameof(AssetSubSystem)} instance is missing.");

                return false;
            }

            if (!assetSubSystem.Has(assetKey))
            {
                Log.Error(typeof(NetworkSpawner), $"Asset '{assetKey}' is not available through the async runtime-loading path required for synchronized spawning.");

                return false;
            }

            AssetSynchronizer synchronizer = AssetSynchronizer.Instance;

            if (!synchronizer)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot ensure addressable load because AssetSynchronizer instance is missing.");

                return false;
            }

            if (await synchronizer.EnsureLoadedOnAllClientsAsync(assetKey))
            {
                return true;
            }

            Log.Error(typeof(NetworkSpawner), $"Failed to ensure addressable asset '{assetKey}' is loaded on all clients before spawning.");

            return false;
        }

        /// <summary>
        /// Registers a successfully spawned addressable instance with the active asset registry.
        /// Assets outside the async runtime-loading path are ignored because they do not participate in synchronized
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

            AssetKey assetKey = new(assetReference.Database, assetReference.Id);

            // Only assets that use the async synchronized loading path are mirrored through the network residency registry.
            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                return;
            }

            if (!assetSubSystem.Has(assetKey))
            {
                return;
            }

            NetworkAssetRegistry.Register(assetKey, networkObject);
        }

        /// <summary>
        /// Releases the shared network residency claim when a synchronized preload succeeded
        /// but the server could not turn that asset into a live spawned world object.
        /// </summary>
        private static void ReleaseFailedAddressableSpawn(AssetKey assetKey)
        {
            AssetSynchronizer synchronizer = AssetSynchronizer.Instance;

            if (synchronizer)
            {
                synchronizer.SynchronizeUnload(assetKey);

                return;
            }

            if (TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                assetSubSystem.ReleaseNetworkAsset(assetKey);
            }
        }

        private static bool TryGetAssetSubSystem([CanBeNull] out AssetSubSystem assetSubSystem)
        {
            assetSubSystem = AssetSubSystem.Instance;

            return assetSubSystem;
        }
    }
}
