using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Ref-counted view of an asset loaded through <see cref="AssetProvider"/>.
    /// Dispose/Release decrements the shared count; Addressables unload happens at zero.
    /// </summary>
    public sealed class AssetHandle<TAsset> : IDisposable
        where TAsset : Object
    {
        private readonly string _key;
        private bool _released;

        internal AssetHandle([NotNull] string key, [NotNull] TAsset asset)
        {
            _key = key;
            Asset = asset;
        }

        [NotNull]
        public TAsset Asset { get; }

        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            AssetProvider.ReleaseShared(_key);
        }

        public void Dispose()
        {
            Release();
        }
    }
}
