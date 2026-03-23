using Coimbra;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Helper for spawning networked objects while ensuring assets
    /// are loaded on all clients before the spawn occurs.
    /// </summary>
    public static class NetworkSpawner
    {
        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance using FishNet.
        /// This is intended for prefabs that are guaranteed to be present on all clients
        /// (eg. scene objects or directly-referenced resources).
        ///
        /// This overload does not perform any asset synchronization. To ensure an
        /// asset is loaded on all clients before spawning,
        /// use the overload that also takes an <see cref="ObjectAssetReference"/>.
        /// </summary>
        public static Task SpawnAsync(NetworkObject networkObject, NetworkConnection ownerConnection = null)
        {
            return SpawnAsync(networkObject, null, ownerConnection);
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="networkObject">The already-instantiated network object to spawn.</param>
        /// <param name="assetReference">
        /// Optional asset reference for the prefab this object belongs to.
        /// When provided, the asset will be synchronized and loaded on all clients before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(
            NetworkObject networkObject,
            [CanBeNull] ObjectAssetReference assetReference,
            NetworkConnection ownerConnection = null)
        {
            if (!InstanceFinder.IsServer)
            {
                Log.Error(typeof(NetworkSpawner), "NetworkSpawner.SpawnAsync(NetworkObject, ObjectAssetReference) can only be called on the server.");

                return;
            }

            if (!networkObject)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the instance is null.");

                return;
            }

            string key = assetReference ? assetReference.Id : null;

            if (assetReference)
            {
                NetworkBarrier barrier = NetworkBarrier.Instance;

                if (!barrier)
                {
                    Log.Error(typeof(NetworkSpawner), $"Cannot ensure asset load for '{key}' because {nameof(NetworkBarrier)} instance is missing.");

                    return;
                }

                if (!await barrier.EnsureAllClientsReadyAsync(key))
                {
                    Log.Error(typeof(NetworkSpawner), $"Failed to ensure asset '{key}' is loaded on all clients before spawning.");

                    return;
                }
            }

            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

            // Track network instance for late-join manifest. AssetLifecycleTracker keeps
            // the asset resident via InstanceLifetimeTracker on the instance.
            if (assetReference)
            {
                if (!networkObject.gameObject.TryGetComponent<InstanceLifetimeTracker>(out _))
                {
                    networkObject.gameObject.AddComponent<InstanceLifetimeTracker>().Initialize(key);
                }

                NetworkBarrier.Instance?.TrackNetworkInstance(key);
            }
        }

        /// <summary>
        /// Spawns a networked object from an <see cref="ObjectAssetReference"/>.
        ///
        /// Flow:
        /// - Acquires a handle to the prefab via <see cref="AssetSubSystem"/>.
        /// - Uses <see cref="NetworkBarrier"/> to ensure it is loaded on all clients before spawning.
        /// - Instantiates the loaded prefab and spawns it using FishNet.
        /// - Registers the instance with <see cref="NetworkBarrier"/> for late-join tracking.
        /// </summary>
        /// <param name="assetReference">Asset reference for the prefab to spawn.</param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        /// <returns>The spawned <see cref="NetworkObject"/>, or <c>null</c> if spawn failed.</returns>
        public static async Task<NetworkObject> SpawnAsync(
            ObjectAssetReference assetReference,
            NetworkConnection ownerConnection = null)
        {
            if (!InstanceFinder.IsServer)
            {
                Log.Error(typeof(NetworkSpawner), "NetworkSpawner.SpawnAsync(ObjectAssetReference) can only be called on the server.");

                return null;
            }

            if (!assetReference)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the asset reference is null.");

                return null;
            }

            string key = assetReference.Id;

            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot spawn '{key}' because {nameof(AssetSubSystem)} instance is missing.");

                return null;
            }

            // 1. Acquire prefab handle (keeps bundle alive through the entire operation).
            AssetHandle<GameObject> spawnHandle = await assetSubSystem.AcquireAsync<GameObject>(key);

            if (spawnHandle?.Asset == null)
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to load prefab for asset '{key}'.");
                spawnHandle?.Dispose();

                return null;
            }

            // 2. Barrier: ensure all clients have the asset.
            NetworkBarrier barrier = NetworkBarrier.Instance;

            if (!barrier || !await barrier.EnsureAllClientsReadyAsync(key))
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to ensure asset '{key}' is loaded on all clients before spawning.");
                spawnHandle.Dispose();

                return null;
            }

            // 3. Instantiate + spawn.
            GameObject instance = Object.Instantiate(spawnHandle.Asset);

            if (!instance || !instance.TryGetComponent(out NetworkObject networkObject))
            {
                Log.Error(typeof(NetworkSpawner), $"Loaded prefab for asset '{key}' does not contain a NetworkObject component.");
                instance.Dispose(true);
                spawnHandle.Dispose();
                barrier.BroadcastUnload(key);

                return null;
            }

            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);

            // 4. Register with NetworkBarrier for late-join tracking.
            NetworkBarrier.Instance?.TrackNetworkInstance(key);

            // 5. Release spawn handle — AssetLifecycleTracker keeps the asset resident
            // via InstanceLifetimeTracker on the instantiated copy.
            spawnHandle.Dispose();

            return networkObject;
        }

        private static bool TryGetAssetSubSystem(out AssetSubSystem assetSubSystem)
        {
            assetSubSystem = SubSystems.Get<AssetSubSystem>();

            return assetSubSystem;
        }
    }
}