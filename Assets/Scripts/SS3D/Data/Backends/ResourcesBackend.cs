using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Concrete <see cref="IAssetBackend"/> wrapping <see cref="UnityEngine.Resources"/>.
    /// The key is the Resources-relative path (e.g. "Prefabs/MyPrefab").
    /// No initialization or catalog lookup is required.
    /// </summary>
    internal sealed class ResourcesBackend : IAssetBackend
    {
        /// <inheritdoc/>>
        public event Action<string, Object> OnLoaded;

        /// <inheritdoc/>>
        public event Action<string> OnUnloaded;

        private readonly Dictionary<string, Object> _loaded = new();

        /// <inheritdoc/>
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task<Object> LoadAsync([NotNull] string guid, [NotNull] string resolvedKey)
        {
            ResourceRequest request = Resources.LoadAsync<Object>(resolvedKey);

            while (!request.isDone)
            {
                await Task.Yield();
            }

            if (request.asset)
            {
                _loaded[guid] = request.asset;
                OnLoaded?.Invoke(guid, request.asset);
            }

            return request.asset;
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string guid, [NotNull] string resolvedKey)
        {
            if (_loaded.Remove(guid, out Object asset) && asset is not GameObject)
            {
                Resources.UnloadAsset(asset);
                OnUnloaded?.Invoke(guid);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (Object asset in _loaded.Values.Where(asset => asset is not GameObject))
            {
                Resources.UnloadAsset(asset);
            }

            _loaded.Clear();
        }
    }
}