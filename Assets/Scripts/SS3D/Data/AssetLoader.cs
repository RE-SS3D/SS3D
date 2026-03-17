using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
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
    /// Low-level Addressables loader for SS3D assets.
    /// This class owns the concrete backend-specific load and unload interaction with Addressables.
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// Tracks one backend-specific Addressables load entry for a GUID.
        /// </summary>
        private sealed class LoaderRecord
        {
            internal LoaderRecord(AsyncOperationHandle<Object> loadingOperation)
            {
                LoadingOperation = loadingOperation;
            }

            internal AsyncOperationHandle<Object> LoadingOperation { get; }

            internal Task<Object> LoadTask { get; set; }
        }

        internal static event Action<string, Object> OnAssetLoaded;

        internal static event Action<string> OnAssetUnloaded;

        private static readonly Dictionary<string, LoaderRecord> LoaderRecords = new();

        /// <summary>
        /// Checks whether the loader currently holds a successful Addressables handle for the given GUID.
        /// </summary>
        internal static bool IsAssetLoaded([NotNull] string guid) => LoaderRecords.TryGetValue(guid, out LoaderRecord loaderRecord)
            && loaderRecord.LoadingOperation is
            {
                IsDone: true,
                Status: AsyncOperationStatus.Succeeded,
            };

        /// <summary>
        /// Loads a resolved Addressables reference using the configured backend.
        /// </summary>
        [ItemCanBeNull]
        internal static async Task<Object> LoadAsync([CanBeNull] AssetReference reference)
        {
            if (reference == null)
            {
                Log.Warning(typeof(AssetLoader), "Cannot load asset because the resolved AssetReference is null.");

                return null;
            }

            string guid = reference.AssetGUID;

            if (LoaderRecords.TryGetValue(guid, out LoaderRecord loaderRecord))
            {
                return await loaderRecord.LoadTask;
            }

            AsyncOperationHandle<Object> loadingOperation = reference.LoadAssetAsync<Object>();
            loaderRecord = new(loadingOperation);
            LoaderRecords.Add(guid, loaderRecord);
            loaderRecord.LoadTask = CompleteLoadAsync(guid, loaderRecord);

            return await loaderRecord.LoadTask;
        }

        /// <summary>
        /// Releases the backend-specific load entry for the given GUID.
        /// </summary>
        internal static bool Unload([NotNull] string guid)
        {
            if (string.IsNullOrWhiteSpace(guid) || !LoaderRecords.TryGetValue(guid, out LoaderRecord loaderRecord))
            {
                return false;
            }

            bool wasLoaded = loaderRecord.LoadingOperation is
            {
                IsDone: true,
                Status: AsyncOperationStatus.Succeeded,
            };

            if (loaderRecord.LoadingOperation.IsValid())
            {
                Addressables.Release(loaderRecord.LoadingOperation);
            }

            LoaderRecords.Remove(guid);

            if (wasLoaded)
            {
                try
                {
                    OnAssetUnloaded?.Invoke(guid);
                }
                catch (Exception e)
                {
                    Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the AssetUnloaded event.");
                }
            }

            return true;
        }

        /// <summary>
        /// Releases the backend-specific load entry for the given resolved Addressables reference.
        /// </summary>
        internal static bool Unload([CanBeNull] AssetReference reference) => reference != null && Unload(reference.AssetGUID);

        /// <summary>
        /// Releases all backend-specific loader state, typically during reset/error paths.
        /// </summary>
        internal static void Clear()
        {
            foreach ((string _, LoaderRecord loaderRecord) in LoaderRecords)
            {
                if (loaderRecord.LoadingOperation.IsValid())
                {
                    Addressables.Release(loaderRecord.LoadingOperation);
                }
            }

            LoaderRecords.Clear();
        }

        // TODO: Migrate callers from AssetLoader.Get(...) to AssetSubSystem.Get(...) and remove this compatibility wrapper.
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object
        {
            AssetSubSystem assetSubSystem = SubSystems.Get<AssetSubSystem>();

            return assetSubSystem ? assetSubSystem.Get<TAsset>(databaseId, assetId) : null;
        }

        // TODO: Migrate callers from AssetLoader.GetAsync(...) to AssetSubSystem.GetAsync(...) and remove this compatibility wrapper.
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] ObjectAssetReference reference)
            where TAsset : class
        {
            AssetSubSystem assetSubSystem = SubSystems.Get<AssetSubSystem>();

            return assetSubSystem ? await assetSubSystem.GetAsync<TAsset>(reference) : null;
        }

        /// <summary>
        /// Converts a backend-loaded object into the requested asset type.
        /// </summary>
        [CanBeNull]
        internal static TAsset CastAsset<TAsset>([CanBeNull] Object obj)
            where TAsset : class
        {
            if (obj is GameObject gameObject && typeof(TAsset) != typeof(GameObject))
            {
                return gameObject.GetComponent<TAsset>();
            }

            return obj as TAsset;
        }

        [ItemCanBeNull]
        private static async Task<Object> CompleteLoadAsync([NotNull] string guid, [NotNull] LoaderRecord loaderRecord)
        {
            Object loadedAsset = await loaderRecord.LoadingOperation.Task;

            if (!LoaderRecords.TryGetValue(guid, out LoaderRecord currentRecord) || !ReferenceEquals(currentRecord, loaderRecord))
            {
                return loadedAsset;
            }

            if (loaderRecord.LoadingOperation is
                {
                    IsDone: true,
                    Status: AsyncOperationStatus.Failed,
                })
            {
                Addressables.Release(loaderRecord.LoadingOperation);
                LoaderRecords.Remove(guid);

                return null;
            }

            try
            {
                OnAssetLoaded?.Invoke(guid, loadedAsset);
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the AssetLoaded event.");
            }

            return loadedAsset;
        }
    }
}
