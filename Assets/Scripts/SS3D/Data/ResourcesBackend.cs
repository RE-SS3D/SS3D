using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        private readonly Dictionary<string, Object> _loaded = new();

        /// <inheritdoc/>
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task<Object> LoadAsync([NotNull] string key)
        {
            ResourceRequest request = Resources.LoadAsync<Object>(key);

            while (!request.isDone)
            {
                await Task.Yield();
            }

            if (request.asset != null)
            {
                _loaded[key] = request.asset;
            }

            return request.asset;
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string key)
        {
            if (_loaded.Remove(key, out Object asset) && asset is not GameObject)
            {
                Resources.UnloadAsset(asset);
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