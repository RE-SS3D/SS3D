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
        public static void Spawn(NetworkBehaviour networkBehaviour, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (ValidateNetworkBehaviour(networkBehaviour, out NetworkObject networkObject))
            {
                Spawn(networkObject, ownerConnection);
            }
            else
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot spawn, invalid NetworkBehaviour {networkBehaviour.name}");
            }
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance using FishNet.
        /// This is intended for prefabs that are guaranteed to be present on all clients
        /// (eg. scene objects or directly-referenced resources).
        ///
        /// This overload does not perform any asset synchronization. To ensure an
        /// asset is loaded on all clients before spawning,
        /// use the overload that also takes an <see cref="ObjectAssetReference"/>.
        /// </summary>
        public static void Spawn(NetworkObject networkObject, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            InstanceFinder.ServerManager.Spawn(networkObject, ownerConnection);
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance using FishNet.
        /// This is intended for prefabs that are guaranteed to be present on all clients
        /// (eg. scene objects or directly-referenced resources).
        ///
        /// This overload does not perform any asset synchronization. To ensure an
        /// asset is loaded on all clients before spawning,
        /// use the overload that also takes an <see cref="ObjectAssetReference"/>.
        /// </summary>
        public static void Spawn(GameObject gameObject, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            InstanceFinder.ServerManager.Spawn(gameObject, ownerConnection);
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="networkBehaviour">
        /// The behaviour whose owning <see cref="NetworkObject"/> will be spawned.
        /// </param>
        /// <param name="assetReference">
        /// Asset reference used to derive the synchronization key before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(
            NetworkBehaviour networkBehaviour,
            [CanBeNull] ObjectAssetReference assetReference,
            [CanBeNull] NetworkConnection ownerConnection = null
        )
        {
            if (ValidateObjectAssetReference(assetReference, out string key))
            {
                if (ValidateNetworkBehaviour(networkBehaviour, out NetworkObject networkObject))
                {
                    await SpawnAsync(networkObject, key, ownerConnection);
                }
                else
                {
                    Log.Error(typeof(NetworkSpawner), $"Cannot spawn {key}, invalid NetworkBehaviour");
                }
            }
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="networkBehaviour">
        /// The behaviour whose owning <see cref="NetworkObject"/> will be spawned.
        /// </param>
        /// <param name="key">
        /// Optional asset key used by <see cref="NetworkBarrier"/> to ensure clients
        /// have loaded the asset before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(NetworkBehaviour networkBehaviour, [CanBeNull] string key, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (ValidateNetworkBehaviour(networkBehaviour, out NetworkObject networkObject))
            {
                await SpawnAsync(networkObject, key, ownerConnection);
            }
            else
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot spawn {key}, invalid NetworkBehaviour");
            }
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
        public static async Task SpawnAsync(NetworkObject networkObject, [CanBeNull] ObjectAssetReference assetReference, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (ValidateObjectAssetReference(assetReference, out string key))
            {
                await SpawnAsync(networkObject, key, ownerConnection);
            }
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="networkObject">The already-instantiated network object to spawn.</param>
        /// <param name="key">
        /// Optional asset key used by <see cref="NetworkBarrier"/> to ensure clients
        /// have loaded the asset before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(NetworkObject networkObject, [CanBeNull] string key, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (!CanSpawn(key))
            {
                return;
            }

            if (!networkObject)
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot spawn {key} network object because the instance is null.");

                return;
            }

            NetworkBarrier barrier = NetworkBarrier.Instance;

            if (!barrier || !await barrier.EnsureAllClientsReadyAsync(key))
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to ensure asset '{key}' is loaded on all clients before spawning.");

                return;
            }

            Spawn(networkObject, ownerConnection);

            // Track network instance for late-join manifest. AssetLifecycleTracker keeps
            // the asset resident via InstanceLifetimeTracker on the instance.
            if (!networkObject.gameObject.TryGetComponent<InstanceLifetimeTracker>(out _))
            {
                networkObject.gameObject.AddComponent<InstanceLifetimeTracker>().Initialize(key);
            }

            NetworkBarrier.Instance?.TrackNetworkInstance(key);
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="assetReference">
        ///     Optional asset reference for the prefab this object belongs to.
        ///     When provided, the asset will be synchronized and loaded on all clients before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(GameObject gameObject, [CanBeNull] ObjectAssetReference assetReference, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (ValidateObjectAssetReference(assetReference, out string key))
            {
                await SpawnAsync(gameObject, key, ownerConnection);
            }
        }

        /// <summary>
        /// Spawns an existing <see cref="NetworkObject"/> instance, optionally
        /// ensuring that its backing asset is loaded on all clients before the spawn occurs.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="key">
        ///     Optional asset key used by <see cref="NetworkBarrier"/> to ensure clients
        ///     have loaded the asset before spawning.
        /// </param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        public static async Task SpawnAsync(GameObject gameObject, [CanBeNull] string key, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (!CanSpawn(key))
            {
                return;
            }

            if (!gameObject)
            {
                Log.Error(typeof(NetworkSpawner), $"Cannot spawn {key} network object because the instance is null.");

                return;
            }

            NetworkBarrier barrier = NetworkBarrier.Instance;

            if (!barrier || !await barrier.EnsureAllClientsReadyAsync(key))
            {
                Log.Error(typeof(NetworkSpawner), $"Failed to ensure asset '{key}' is loaded on all clients before spawning.");

                return;
            }

            Spawn(gameObject, ownerConnection);

            // Track network instance for late-join manifest. AssetLifecycleTracker keeps
            // the asset resident via InstanceLifetimeTracker on the instance.
            if (!gameObject.TryGetComponent<InstanceLifetimeTracker>(out _))
            {
                gameObject.AddComponent<InstanceLifetimeTracker>().Initialize(key);
            }

            NetworkBarrier.Instance?.TrackNetworkInstance(key);
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
        [ItemCanBeNull]
        public static async Task<NetworkObject> SpawnAsync(ObjectAssetReference assetReference, NetworkConnection ownerConnection = null)
        {
            if (ValidateObjectAssetReference(assetReference, out string key))
            {
                return await SpawnAsync(key, ownerConnection);
            }

            return null;
        }

        /// <summary>
        /// Spawns a networked object from an asset key.
        /// 
        /// Flow:
        /// - Acquires a handle to the prefab via <see cref="AssetSubSystem"/>.
        /// - Uses <see cref="NetworkBarrier"/> to ensure it is loaded on all clients before spawning.
        /// - Instantiates the loaded prefab and spawns it using FishNet.
        /// - Registers the instance with <see cref="NetworkBarrier"/> for late-join tracking.
        /// </summary>
        /// <param name="key">Asset key of the prefab to spawn.</param>
        /// <param name="ownerConnection">Optional owner connection for the spawned object.</param>
        /// <returns>The spawned <see cref="NetworkObject"/>, or <c>null</c> if spawn failed.</returns>
        [ItemCanBeNull]
        public static async Task<NetworkObject> SpawnAsync(string key, [CanBeNull] NetworkConnection ownerConnection = null)
        {
            if (!CanSpawn(key))
            {
                return null;
            }

            // 1. Acquire prefab handle (keeps bundle alive through the entire operation).
            AssetHandle<NetworkObject> spawnHandle = await new AssetRequest<NetworkObject>(key).LoadAsync();

            if (!spawnHandle)
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
            NetworkObject instance = Object.Instantiate(spawnHandle.Asset);

            if (!instance)
            {
                Log.Error(typeof(NetworkSpawner), $"Loaded prefab for asset '{key}' does not contain a NetworkObject component.");
                instance.Dispose(true);
                spawnHandle.Dispose();
                barrier.BroadcastUnload(key);

                return null;
            }

            Spawn(instance, ownerConnection);

            // 4. Register with NetworkBarrier for late-join tracking.
            NetworkBarrier.Instance?.TrackNetworkInstance(key);

            // 5. Release spawn handle — AssetLifecycleTracker keeps the asset resident
            // via InstanceLifetimeTracker on the instantiated copy.
            spawnHandle.Dispose();

            return instance;
        }

        /// <summary>
        /// Validates a <see cref="NetworkBehaviour"/> and extracts its owning
        /// <see cref="NetworkObject"/>.
        /// </summary>
        /// <param name="networkBehaviour">The behaviour to validate.</param>
        /// <param name="networkObject">
        /// When this method returns, contains the associated network object if validation
        /// succeeded; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if <paramref name="networkBehaviour"/> is valid; otherwise <c>false</c>.</returns>
        private static bool ValidateNetworkBehaviour(NetworkBehaviour networkBehaviour, [CanBeNull] out NetworkObject networkObject)
        {
            networkObject = null;

            if (networkBehaviour)
            {
                networkObject = networkBehaviour.NetworkObject ? networkBehaviour.NetworkObject : networkBehaviour.GetComponent<NetworkObject>();
            }

            return networkObject;
        }

        /// <summary>
        /// Validates an <see cref="ObjectAssetReference"/> and extracts its asset key.
        /// </summary>
        /// <param name="assetReference">The asset reference to validate.</param>
        /// <param name="key">
        /// When this method returns, contains the asset key if validation succeeded;
        /// otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if <paramref name="assetReference"/> is valid; otherwise <c>false</c>.</returns>
        private static bool ValidateObjectAssetReference([NotNull] ObjectAssetReference assetReference, out string key)
        {
            if (assetReference)
            {
                key = assetReference.Id;

                return true;
            }

            Log.Error(typeof(NetworkSpawner), $"Cannot spawn {assetReference.name}\\{assetReference.Id}, invalid ObjectAssetReference");
            key = null;

            return false;
        }

        private static bool CanSpawn(string key)
        {
            if (!InstanceFinder.IsServer)
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the instance is not on the server");

                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                Log.Error(typeof(NetworkSpawner), "Cannot spawn network object because the key is null or whitespace.");

                return false;
            }

            return true;
        }
    }
}