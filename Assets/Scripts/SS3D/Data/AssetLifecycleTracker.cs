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
            AssetSubSystem.OnAssetLoaded += TrackLoadedAsset;
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
            AssetSubSystem.OnAssetLoaded -= TrackLoadedAsset;
            InstanceLifetimeTracker.OnInstantiated -= HandleInstanceCreated;
            InstanceLifetimeTracker.OnReleased -= HandleInstanceReleased;
            _refCounts.Clear();
        }

        /// <summary>
        /// Arms the loaded GameObject prefab's <see cref="InstanceLifetimeTracker"/>
        /// so that future <see cref="Object.Instantiate(Object)"/> copies inherit the armed state via serialization.
        /// </summary>
        internal void TrackLoadedAsset(string key, Object asset)
        {
            if (asset is not GameObject go || !_refCounts.ContainsKey(key))
            {
                return;
            }

            if (!go.TryGetComponent(out InstanceLifetimeTracker tracker))
            {
                Debug.LogWarning($"Asset prefab '{go.name}' is missing {nameof(InstanceLifetimeTracker)}. Runtime fallback added it for GUID '{key}'. Run the asset prefab migration before removing compatibility fallback.", go);
                tracker = go.AddComponent<InstanceLifetimeTracker>();
            }

            tracker.Initialize(key);
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