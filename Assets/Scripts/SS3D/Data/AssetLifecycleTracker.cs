using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Single source of truth for asset lifecycle decisions.
    /// Maintains one ref count per asset combining handle references and live Unity instances.
    /// When count reaches zero, tells the provider to unload.
    /// </summary>
    internal sealed class AssetLifecycleTracker
    {
        private readonly Dictionary<string, int> _refCounts = new();
        private readonly IAssetProvider _provider;

        internal AssetLifecycleTracker(IAssetProvider provider)
        {
            _provider = provider;
            AssetSubSystem.OnAssetLoaded += HandleAssetLoaded;
            InstanceLifetimeTracker.OnInstantiated += HandleInstanceCreated;
            InstanceLifetimeTracker.OnReleased += HandleInstanceReleased;
        }

        /// <summary>
        /// Increments ref count. Called synchronously by <see cref="AssetSubSystem"/> BEFORE await.
        /// </summary>
        internal void TrackAcquire(string key)
        {
            _refCounts[key] = _refCounts.GetValueOrDefault(key) + 1;
        }

        /// <summary>
        /// Decrements ref count. Injected as the release delegate into <see cref="AssetHandle{T}"/>.
        /// </summary>
        internal void TrackRelease(string key)
        {
            if (!_refCounts.TryGetValue(key, out int count))
            {
                return;
            }

            count--;

            if (count <= 0)
            {
                _refCounts.Remove(key);
                _provider.Unload(key);
            }
            else
            {
                _refCounts[key] = count;
            }
        }

        internal void Shutdown()
        {
            AssetSubSystem.OnAssetLoaded -= HandleAssetLoaded;
            InstanceLifetimeTracker.OnInstantiated -= HandleInstanceCreated;
            InstanceLifetimeTracker.OnReleased -= HandleInstanceReleased;
            _refCounts.Clear();
        }

        /// <summary>
        /// Injects <see cref="InstanceLifetimeTracker"/> onto loaded GameObject prefabs
        /// so that future <see cref="Object.Instantiate(Object)"/> copies inherit it via serialization.
        /// </summary>
        private void HandleAssetLoaded(string key, Object asset)
        {
            if (asset is GameObject go && _refCounts.ContainsKey(key))
            {
                go.AddComponent<InstanceLifetimeTracker>().Initialize(key);
            }
        }

        private void HandleInstanceCreated(string key)
        {
            _refCounts[key] = _refCounts.GetValueOrDefault(key) + 1;
        }

        private void HandleInstanceReleased(string key)
        {
            TrackRelease(key);
        }
    }
}