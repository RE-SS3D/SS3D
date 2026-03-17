using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Data;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Tracks the active network-spawned addressable assets for the current server session.
    /// </summary>
    internal static class NetworkAssetRegistry
    {
        /// <summary>
        /// Raised when the last live spawned instance for an addressable asset has been released.
        /// Consumers typically use this to trigger synchronized unload across clients.
        /// </summary>
        internal static event Action<AssetKey> OnAssetNoLongerActive;

        private static readonly Dictionary<AssetKey, int> ActiveAssetRefCounts = new();

        static NetworkAssetRegistry()
        {
            // Instance lifetime is emitted by the tracker component on each spawned object.
            AssetLifetimeTracker.OnReleased += HandleAssetReleased;
        }

        /// <summary>
        /// Registers a spawned addressable network object as an active user of its backing asset.
        /// Multiple spawned instances of the same prefab share a single asset key and are refcounted.
        /// </summary>
        /// <param name="databaseId">Database containing the addressable prefab.</param>
        /// <param name="assetId">Asset identifier within the database.</param>
        /// <param name="networkObject">Spawned network instance that should contribute to the active refcount.</param>
        internal static void Register([CanBeNull] string databaseId, [CanBeNull] string assetId, [CanBeNull] NetworkObject networkObject)
        {
            Register(new(databaseId, assetId), networkObject);
        }

        /// <summary>
        /// Registers a spawned addressable network object as an active user of its backing asset.
        /// Multiple spawned instances of the same prefab share a single asset key and are refcounted.
        /// </summary>
        internal static void Register(AssetKey assetKey, [CanBeNull] NetworkObject networkObject)
        {
            if (!assetKey.IsValid)
            {
                Log.Warning(typeof(NetworkAssetRegistry), $"Ignoring invalid active asset registration '{assetKey}'.");

                return;
            }

            if (!AssetLoader.Has(assetKey))
            {
                Log.Warning(typeof(NetworkAssetRegistry), $"Ignoring active asset registration for '{assetKey}' because it is outside the async synchronized loading path.");

                return;
            }

            if (!networkObject)
            {
                Log.Warning(typeof(NetworkAssetRegistry), $"Cannot register active asset '{assetKey}' because the network object is null.");

                return;
            }

            if (ActiveAssetRefCounts.TryGetValue(assetKey, out int activeCount))
            {
                ActiveAssetRefCounts[assetKey] = activeCount + 1;
            }
            else
            {
                ActiveAssetRefCounts.Add(assetKey, 1);
            }

            AssetLifetimeTracker tracker = networkObject.GetComponent<AssetLifetimeTracker>();

            if (!tracker)
            {
                // Each spawned instance needs its own tracker so disable/destroy events can
                // release exactly one refcount contribution for that instance.
                tracker = networkObject.gameObject.AddComponent<AssetLifetimeTracker>();
            }

            tracker.Initialize(assetKey);
        }

        /// <summary>
        /// Returns a snapshot of currently active addressable asset keys.
        /// This is used for late-join replay and intentionally does not expose refcounts.
        /// </summary>
        [NotNull]
        internal static AssetKey[] GetActiveAssets() => ActiveAssetRefCounts.Keys.ToArray();

        /// <summary>
        /// Clears all server-side active asset state, typically during shutdown/reset paths.
        /// </summary>
        internal static void Clear()
        {
            ActiveAssetRefCounts.Clear();
        }

        /// <summary>
        /// Handles one instance-level release notification from <see cref="AssetLifetimeTracker"/>
        /// and decrements the shared refcount for the corresponding addressable asset.
        /// </summary>
        private static void HandleAssetReleased(AssetKey assetKey)
        {
            if (!assetKey.IsValid)
            {
                return;
            }

            if (!ActiveAssetRefCounts.TryGetValue(assetKey, out int activeCount))
            {
                return;
            }

            if (activeCount > 1)
            {
                // Other live instances still depend on this asset, so only the count changes.
                ActiveAssetRefCounts[assetKey] = activeCount - 1;

                return;
            }

            // The final live instance is gone, so the asset can leave the active manifest.
            ActiveAssetRefCounts.Remove(assetKey);
            OnAssetNoLongerActive?.Invoke(assetKey);
        }
    }
}
