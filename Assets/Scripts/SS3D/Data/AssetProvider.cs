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
        }

        /// <inheritdoc/>
        public event Action<string, Object> OnLoaded;

        /// <inheritdoc/>
        public event Action<string> OnUnloaded;

        private readonly Dictionary<string, Record> _records = new();
        private readonly Action<string> _releaseCallback;

        internal AssetProvider([NotNull] Action<string> releaseCallback)
        {
            _releaseCallback = releaseCallback;
        }

        /// <inheritdoc/>
        [ItemNotNull]
        public async Task<AssetHandle<T>> AcquireAsync<T>([NotNull] string key, [NotNull] IAssetBackend backend)
            where T : class
        {
            if (!_records.TryGetValue(key, out Record record))
            {
                record = new() { Backend = backend, LoadTask = LoadCoreAsync(key, backend) };
                _records[key] = record;
            }

            Object obj = await record.LoadTask;

            return new(key, CastAsset<T>(obj), _releaseCallback);
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string key)
        {
            if (!_records.Remove(key, out Record record))
            {
                return;
            }

            if (record.LoadTask is { IsCompletedSuccessfully: true })
            {
                record.Backend.Unload(key);
            }

            OnUnloaded?.Invoke(key);
        }

        /// <inheritdoc/>
        public bool IsLoaded([NotNull] string key)
            => _records.TryGetValue(key, out Record r) && r.LoadTask is { IsCompletedSuccessfully: true };

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach ((string key, Record record) in _records)
            {
                if (record.LoadTask is { IsCompletedSuccessfully: true })
                {
                    record.Backend.Unload(key);
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

        private async Task<Object> LoadCoreAsync([NotNull] string key, [NotNull] IAssetBackend backend)
        {
            Object asset = await backend.LoadAsync(key);

            if (asset)
            {
                OnLoaded?.Invoke(key, asset);
            }

            return asset;
        }
    }
}