using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Pure asset loader. Deduplicates concurrent loads and routes unload calls
    /// back to the backend that originally loaded each asset.
    /// Does not make lifecycle decisions — an external owner decides when to unload.
    /// </summary>
    internal sealed class AssetProvider : IAssetProvider
    {
        private sealed class Record
        {
            public Task<Object> LoadTask { get; init; }

            public IAssetBackend Backend { get; init; }

            public string ResolvedKey { get; init; }
        }

        private readonly Dictionary<string, Record> _records = new();
        private readonly Action<string> _releaseCallback;

        internal AssetProvider([NotNull] Action<string> releaseCallback)
        {
            _releaseCallback = releaseCallback;
        }

        /// <inheritdoc/>
        [ItemNotNull]
        public async Task<AssetHandle<T>> AcquireAsync<T>([NotNull] string guid, [NotNull] string resolvedKey, [NotNull] IAssetBackend backend)
            where T : class
        {
            if (!_records.TryGetValue(guid, out Record record))
            {
                record = new() { Backend = backend, ResolvedKey = resolvedKey, LoadTask = LoadAsync(guid, resolvedKey, backend) };
                _records[guid] = record;
            }

            Object obj = await record.LoadTask;

            return new(guid, CastAsset<T>(obj), _releaseCallback);
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string guid)
        {
            if (!_records.Remove(guid, out Record record))
            {
                return;
            }

            if (record.LoadTask is { IsCompletedSuccessfully: true })
            {
                record.Backend.Unload(guid, record.ResolvedKey);
            }
        }

        /// <inheritdoc/>
        public bool IsLoaded([NotNull] string guid)
            => _records.TryGetValue(guid, out Record r) && r.LoadTask is { IsCompletedSuccessfully: true };

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach ((string guid, Record record) in _records)
            {
                if (record.LoadTask is { IsCompletedSuccessfully: true })
                {
                    record.Backend.Unload(guid, record.ResolvedKey);
                }
            }

            _records.Clear();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        [CanBeNull]
        private static T CastAsset<T>([CanBeNull] Object obj)
            where T : class
        {
            if (obj is GameObject go && typeof(T) != typeof(GameObject))
            {
                return go.GetComponent<T>();
            }

            return obj as T;
        }

        private async Task<Object> LoadAsync([NotNull] string guid, [NotNull] string resolvedKey, [NotNull] IAssetBackend backend)
        {
            return await backend.LoadAsync(guid, resolvedKey);
        }
    }
}