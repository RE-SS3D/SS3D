using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Tracks live spawned instances per asset key on the server.
    /// Holds an <see cref="IAssetHandle"/> per active asset to keep the backing bundle alive
    /// while any spawned instance of that asset exists in the world.
    /// </summary>
    internal sealed class WorldTracker
    {
        private sealed class ActiveAsset
        {
            internal int InstanceCount;
            internal IAssetHandle Handle;
        }

        /// <summary>
        /// Raised when the last live spawned instance for an asset key is destroyed.
        /// </summary>
        internal event Action<string> OnLastInstanceDestroyed;

        private readonly AssetSubSystem _assetSubSystem;
        private readonly Dictionary<string, ActiveAsset> _activeAssets = new();

        internal WorldTracker([NotNull] AssetSubSystem assetSubSystem)
        {
            _assetSubSystem = assetSubSystem;
            InstanceLifetimeTracker.OnReleased += HandleInstanceReleased;
        }

        internal void Shutdown()
        {
            InstanceLifetimeTracker.OnReleased -= HandleInstanceReleased;

            foreach (ActiveAsset activeAsset in _activeAssets.Values)
            {
                activeAsset.Handle?.Dispose();
            }

            _activeAssets.Clear();
        }

        /// <summary>
        /// Registers a spawned instance. Acquires a store handle on first instance of this key.
        /// Must be called while the spawner's handle is still alive so the ref count never drops to zero.
        /// </summary>
        internal async Task RegisterAsync(
            [NotNull] string key,
            [NotNull] NetworkObject instance)
        {
            if (_activeAssets.TryGetValue(key, out ActiveAsset active))
            {
                active.InstanceCount++;
            }
            else
            {
                // Asset is already loaded (spawner holds a handle), so this completes near-instantly.
                AssetHandle<Object> handle = await _assetSubSystem.AcquireAsync<Object>(key);
                _activeAssets[key] = new ActiveAsset { InstanceCount = 1, Handle = handle };
            }

            InstanceLifetimeTracker tracker = instance.gameObject.AddComponent<InstanceLifetimeTracker>();
            tracker.Initialize(key);
        }

        /// <summary>
        /// Returns a snapshot of currently active asset keys.
        /// Used for late-join replay.
        /// </summary>
        [NotNull]
        internal string[] GetActiveAssets()
        {
            return _activeAssets.Keys.ToArray();
        }

        private void HandleInstanceReleased(string key)
        {
            if (!_activeAssets.TryGetValue(key, out ActiveAsset active))
            {
                return;
            }

            if (active.InstanceCount > 1)
            {
                active.InstanceCount--;

                return;
            }

            _activeAssets.Remove(key);
            active.Handle?.Dispose();
            OnLastInstanceDestroyed?.Invoke(key);
        }
    }
}