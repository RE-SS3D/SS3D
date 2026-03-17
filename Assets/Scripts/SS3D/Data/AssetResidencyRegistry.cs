using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Owns shared Addressables residency records and explicit ownership claims for loaded assets.
    /// </summary>
    internal static class AssetResidencyRegistry
    {
        /// <summary>
        /// Tracks one shared Addressables residency entry for a loaded GUID.
        /// Multiple systems can share the same load task and loaded handle while
        /// each keeping its own ownership claim on the asset.
        /// </summary>
        private sealed class ResidencyRecord
        {
            internal ResidencyRecord(AsyncOperationHandle<Object> loadingOperation)
            {
                LoadingOperation = loadingOperation;
            }

            internal AsyncOperationHandle<Object> LoadingOperation { get; }

            internal Task<Object> LoadTask { get; init; }

            internal HashSet<AssetOwnerToken> Owners { get; } = new();
        }

        internal static event Action<KeyValuePair<string, Object>> OnAssetLoaded;

        internal static event Action<string> OnAssetUnloaded;

        private static readonly Dictionary<string, ResidencyRecord> ResidencyRecords = new();

        internal static bool IsLoaded([NotNull] string guid) => ResidencyRecords.TryGetValue(guid, out ResidencyRecord residencyRecord)
            && residencyRecord.LoadingOperation is
            {
                IsDone: true,
                Status: AsyncOperationStatus.Succeeded,
            };

        [ItemCanBeNull]
        internal static async Task<TAsset> AcquireAsync<TAsset>([NotNull] AssetReference reference, AssetOwnerToken owner)
            where TAsset : class
        {
            if (!owner.IsValid)
            {
                Log.Warning(typeof(AssetResidencyRegistry), $"Cannot acquire residency for GUID '{reference.AssetGUID}' because the owner token is invalid.");

                return null;
            }

            if (!ResidencyRecords.TryGetValue(reference.AssetGUID, out ResidencyRecord residencyRecord))
            {
                AsyncOperationHandle<Object> loadingOperation = reference.LoadAssetAsync<Object>();
                residencyRecord = new(loadingOperation)
                {
                    LoadTask = LoadAsync(reference.AssetGUID, loadingOperation),
                };
                ResidencyRecords.Add(reference.AssetGUID, residencyRecord);
            }

            residencyRecord.Owners.Add(owner);
            Object loadedAsset = await residencyRecord.LoadTask;

            return CastAsset<TAsset>(loadedAsset);
        }

        internal static bool Release([NotNull] string guid, AssetOwnerToken owner)
        {
            if (string.IsNullOrWhiteSpace(guid) || !owner.IsValid)
            {
                return false;
            }

            if (!ResidencyRecords.TryGetValue(guid, out ResidencyRecord residencyRecord))
            {
                return false;
            }

            if (!residencyRecord.Owners.Remove(owner))
            {
                return false;
            }

            if (residencyRecord.Owners.Count == 0 && residencyRecord.LoadingOperation is
                {
                    IsDone: true,
                    Status: AsyncOperationStatus.Succeeded,
                })
            {
                ReleaseResidencyRecord(guid, residencyRecord);
            }

            return true;
        }

        internal static void Clear()
        {
            foreach ((string _, ResidencyRecord residencyRecord) in ResidencyRecords)
            {
                if (residencyRecord.LoadingOperation.IsValid())
                {
                    Addressables.Release(residencyRecord.LoadingOperation);
                }
            }

            ResidencyRecords.Clear();
        }

        [ItemCanBeNull]
        private static async Task<Object> LoadAsync([NotNull] string guid, AsyncOperationHandle<Object> loadingOperation)
        {
            Object loadedAsset = await loadingOperation.Task;

            if (loadingOperation is
                {
                    IsDone: true,
                    Status: AsyncOperationStatus.Failed,
                })
            {
                Addressables.Release(loadingOperation);
                ResidencyRecords.Remove(guid);

                return null;
            }

            try
            {
                OnAssetLoaded?.Invoke(new(guid, loadedAsset));
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetResidencyRegistry), e, "An exception occurred while invoking the AssetLoaded event.");
            }

            if (ResidencyRecords.TryGetValue(guid, out ResidencyRecord residencyRecord) && residencyRecord.Owners.Count == 0)
            {
                ReleaseResidencyRecord(guid, residencyRecord);
            }

            return loadedAsset;
        }

        private static void ReleaseResidencyRecord([NotNull] string guid, [NotNull] ResidencyRecord residencyRecord)
        {
            if (!ResidencyRecords.TryGetValue(guid, out ResidencyRecord currentRecord) || !ReferenceEquals(currentRecord, residencyRecord))
            {
                return;
            }

            Addressables.Release(residencyRecord.LoadingOperation);
            ResidencyRecords.Remove(guid);

            try
            {
                OnAssetUnloaded?.Invoke(guid);
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetResidencyRegistry), e, "An exception occurred while invoking the AssetUnloaded event.");
            }
        }

        private static TAsset CastAsset<TAsset>(Object obj)
            where TAsset : class
        {
            if (obj is GameObject gameObject && typeof(TAsset) != typeof(GameObject))
            {
                return gameObject.GetComponent<TAsset>();
            }

            return obj as TAsset;
        }
    }
}
