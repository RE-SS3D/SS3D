using System.Threading.Tasks;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data.AssetDatabases;

namespace SS3D.Data
{
    /// <summary>
    /// Struct-based fluent builder for asset load requests.
    /// Absorbs subsystem lookup and validation, providing a single extension point
    /// for future options (timeout, network strategy, etc.) without combinatorial overloads.
    /// </summary>
    public readonly struct AssetRequest<T>
        where T : class
    {
        private readonly string _key;

        public AssetRequest([NotNull] string key)
        {
            _key = key;
        }

        public AssetRequest([NotNull] ObjectAssetReference reference)
        {
            _key = reference.Id;
        }

        /// <summary>
        /// Executes the request: resolves the asset subsystem, loads the asset, and returns a ref-counted handle.
        /// Returns <see langword="null"/> if the subsystem is unavailable or the asset is not found.
        /// </summary>
        public async Task<AssetHandle<T>> LoadAsync()
        {
            if (!SubSystems.TryGet(out AssetSubSystem assetSubSystem) || !assetSubSystem)
            {
                return null;
            }

            return await assetSubSystem.AcquireAsync<T>(_key);
        }

        /// <summary>
        /// Executes the request: resolves the asset subsystem, loads the asset, and returns a ref-counted handle.
        /// Returns <see langword="null"/> if the subsystem is unavailable or the asset is not found.
        /// </summary>
        public AssetHandle<T> Load() => LoadAsync().GetAwaiter().GetResult();
    }
}
