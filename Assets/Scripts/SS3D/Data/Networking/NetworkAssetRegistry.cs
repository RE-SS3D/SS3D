using FishNet.Object;
using JetBrains.Annotations;
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
        internal static event Action<string, string> OnAssetNoLongerActive;

        private static readonly Dictionary<(string DatabaseId, string AssetId), int> ActiveAssetRefCounts = new();

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
            if (!IsValidRequest(databaseId, assetId))
            {
                Log.Warning(typeof(NetworkAssetRegistry), $"Ignoring invalid active asset registration. Database: '{databaseId}', Asset: '{assetId}'.");

                return;
            }

            if (!networkObject)
            {
                Log.Warning(typeof(NetworkAssetRegistry), $"Cannot register active asset '{databaseId}/{assetId}' because the network object is null.");

                return;
            }

            (string DatabaseId, string AssetId) key = (databaseId, assetId);

            if (ActiveAssetRefCounts.TryGetValue(key, out int activeCount))
            {
                ActiveAssetRefCounts[key] = activeCount + 1;
            }
            else
            {
                ActiveAssetRefCounts.Add(key, 1);
            }

            AssetLifetimeTracker tracker = networkObject.GetComponent<AssetLifetimeTracker>();

            if (!tracker)
            {
                // Each spawned instance needs its own tracker so disable/destroy events can
                // release exactly one refcount contribution for that instance.
                tracker = networkObject.gameObject.AddComponent<AssetLifetimeTracker>();
            }

            tracker.Initialize(databaseId, assetId);
        }

        /// <summary>
        /// Returns a snapshot of currently active addressable asset keys.
        /// This is used for late-join replay and intentionally does not expose refcounts.
        /// </summary>
        [NotNull]
        internal static (string DatabaseId, string AssetId)[] GetActiveAssets() => ActiveAssetRefCounts.Keys.ToArray();

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
        private static void HandleAssetReleased(string databaseId, string assetId)
        {
            if (!IsValidRequest(databaseId, assetId))
            {
                return;
            }

            (string DatabaseId, string AssetId) key = (databaseId, assetId);

            if (!ActiveAssetRefCounts.TryGetValue(key, out int activeCount))
            {
                return;
            }

            if (activeCount > 1)
            {
                // Other live instances still depend on this asset, so only the count changes.
                ActiveAssetRefCounts[key] = activeCount - 1;

                return;
            }

            // The final live instance is gone, so the asset can leave the active manifest.
            ActiveAssetRefCounts.Remove(key);
            OnAssetNoLongerActive?.Invoke(databaseId, assetId);
        }

        /// <summary>
        /// Ensures the registry only tracks addressable keys that can be safely replayed and unloaded.
        /// </summary>
        private static bool IsValidRequest([CanBeNull] string databaseId, [CanBeNull] string assetId)
        {
            return !string.IsNullOrWhiteSpace(databaseId) && !string.IsNullOrWhiteSpace(assetId);
        }
    }
}
