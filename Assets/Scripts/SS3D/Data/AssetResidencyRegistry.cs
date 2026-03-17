using JetBrains.Annotations;
using SS3D.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Owns asset residency policy and explicit ownership claims for loaded assets.
    /// This class decides when an asset should remain resident and when the backend loader
    /// may unload it, but does not perform the backend-specific load/unload itself.
    /// </summary>
    internal static class AssetResidencyRegistry
    {
        /// <summary>
        /// Tracks one shared residency entry for a GUID.
        /// Multiple logical owners can share the same load task while keeping separate ownership claims.
        /// </summary>
        private sealed class ResidencyRecord
        {
            internal Task<Object> LoadTask { get; set; }

            internal HashSet<AssetOwnerToken> Owners { get; } = new();
        }

        private static readonly Dictionary<string, ResidencyRecord> ResidencyRecords = new();

        internal static bool IsLoaded([NotNull] string guid) => ResidencyRecords.ContainsKey(guid) && AssetLoader.IsAssetLoaded(guid);

        [ItemCanBeNull]
        internal static async Task<TAsset> AcquireAsync<TAsset>([NotNull] AssetReference reference, AssetOwnerToken owner)
            where TAsset : class
        {
            if (!owner.IsValid)
            {
                Log.Warning(typeof(AssetResidencyRegistry), $"Cannot acquire residency for GUID '{reference.AssetGUID}' because the owner token is invalid.");

                return null;
            }

            string guid = reference.AssetGUID;

            if (!ResidencyRecords.TryGetValue(guid, out ResidencyRecord residencyRecord))
            {
                residencyRecord = new();
                residencyRecord.Owners.Add(owner);
                ResidencyRecords.Add(guid, residencyRecord);
                residencyRecord.LoadTask = CompleteAcquireAsync(guid, reference, residencyRecord);
            }
            else
            {
                residencyRecord.Owners.Add(owner);
            }

            Object loadedAsset = await residencyRecord.LoadTask;

            return AssetLoader.CastAsset<TAsset>(loadedAsset);
        }

        internal static bool Release([NotNull] string guid, AssetOwnerToken owner)
        {
            if (string.IsNullOrWhiteSpace(guid) || !owner.IsValid || !ResidencyRecords.TryGetValue(guid, out ResidencyRecord residencyRecord))
            {
                return false;
            }

            if (!residencyRecord.Owners.Remove(owner))
            {
                return false;
            }

            if (residencyRecord.Owners.Count == 0 && residencyRecord.LoadTask.IsCompleted)
            {
                ResidencyRecords.Remove(guid);
                if (AssetLoader.IsAssetLoaded(guid))
                {
                    AssetLoader.Unload(guid);
                }
            }

            return true;
        }

        internal static bool Release([CanBeNull] AssetReference reference, AssetOwnerToken owner) => reference != null && Release(reference.AssetGUID, owner);

        internal static void Clear()
        {
            ResidencyRecords.Clear();
            AssetLoader.Clear();
        }

        [ItemCanBeNull]
        private static async Task<Object> CompleteAcquireAsync([NotNull] string guid, [NotNull] AssetReference reference, [NotNull] ResidencyRecord residencyRecord)
        {
            Object loadedAsset = await AssetLoader.LoadAsync(reference);

            if (loadedAsset == null)
            {
                if (ResidencyRecords.TryGetValue(guid, out ResidencyRecord currentRecord) && ReferenceEquals(currentRecord, residencyRecord))
                {
                    ResidencyRecords.Remove(guid);
                }

                return null;
            }

            if (ResidencyRecords.TryGetValue(guid, out ResidencyRecord activeRecord)
                && ReferenceEquals(activeRecord, residencyRecord)
                && residencyRecord.Owners.Count == 0)
            {
                ResidencyRecords.Remove(guid);
                AssetLoader.Unload(guid);
            }

            return loadedAsset;
        }
    }
}
