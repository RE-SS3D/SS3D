using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Instance-based, ref-counted resource manager.
    /// Tracks loaded assets, deduplicates concurrent loads, and routes release calls
    /// back to the backend that originally loaded each asset.
    /// </summary>
    internal sealed class AssetStore : IAssetStore
    {
        private sealed class Record
        {
            public Task<Object> LoadTask;
            public int RefCount;
            public IAssetBackend Backend;
        }

        private readonly Dictionary<string, Record> _records = new();

        /// <inheritdoc/>
        public event Action<string, Object> OnLoaded;

        /// <inheritdoc/>
        public event Action<string> OnUnloaded;

        /// <inheritdoc/>
        public async Task<AssetHandle<T>> AcquireAsync<T>([NotNull] string key, [NotNull] IAssetBackend backend)
            where T : class
        {
            if (_records.TryGetValue(key, out Record record))
            {
                record.RefCount++;
                Object obj = await record.LoadTask;

                return new AssetHandle<T>(key, CastAsset<T>(obj), Release);
            }

            record = new Record { RefCount = 1, Backend = backend };
            _records.Add(key, record);
            record.LoadTask = CompleteLoadAsync(key, backend, record);

            Object loaded = await record.LoadTask;

            return new AssetHandle<T>(key, CastAsset<T>(loaded), Release);
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

        /// <summary>
        /// Release callback passed to each <see cref="AssetHandle{T}"/> via delegate.
        /// </summary>
        private void Release([NotNull] string key)
        {
            if (!_records.TryGetValue(key, out Record record))
            {
                return;
            }

            record.RefCount--;

            if (record.RefCount <= 0 && record.LoadTask.IsCompleted)
            {
                _records.Remove(key);
                record.Backend.Unload(key);
                OnUnloaded?.Invoke(key);
            }
        }

        private async Task<Object> CompleteLoadAsync([NotNull] string key, [NotNull] IAssetBackend backend, [NotNull] Record record)
        {
            Object loaded = await backend.LoadAsync(key);

            if (_records.TryGetValue(key, out Record current)
                && ReferenceEquals(current, record)
                && record.RefCount <= 0)
            {
                _records.Remove(key);
                backend.Unload(key);

                return null;
            }

            if (loaded != null)
            {
                OnLoaded?.Invoke(key, loaded);
            }

            return loaded;
        }

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
    }
}