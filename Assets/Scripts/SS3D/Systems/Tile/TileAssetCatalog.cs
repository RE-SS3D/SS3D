using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Data.AssetDatabases;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Stable ushort index for <see cref="GenericObjectSo"/> assets loaded by <see cref="TileResourceLoader"/>.
    /// Indices are assigned by sorted <see cref="GenericObjectSo.NameString"/> so server and client agree.
    /// </summary>
    public sealed class TileAssetCatalog
    {
        public const ushort InvalidAssetId = ushort.MaxValue;

        private GenericObjectSo[] _assetsById = Array.Empty<GenericObjectSo>();
        private Dictionary<GenericObjectSo, ushort> _idByAsset = new();

        public bool IsBuilt => _assetsById.Length > 0 || _idByAsset.Count > 0;

        public int Count => _assetsById.Length;

        public void Build(IReadOnlyList<GenericObjectSo> assets)
        {
            _idByAsset.Clear();

            if (assets == null || assets.Count == 0)
            {
                _assetsById = Array.Empty<GenericObjectSo>();
                return;
            }

            if (assets.Count >= InvalidAssetId)
                throw new InvalidOperationException($"Tile asset catalog cannot index {assets.Count} assets in a ushort.");

            List<GenericObjectSo> sorted = assets
                .Where(asset => asset != null)
                .OrderBy(asset => asset.NameString, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _assetsById = new GenericObjectSo[sorted.Count];

            for (ushort id = 0; id < sorted.Count; id++)
            {
                _assetsById[id] = sorted[id];
                _idByAsset[sorted[id]] = id;
            }
        }

        public ushort TryGetAssetId(GenericObjectSo asset)
        {
            if (asset == null)
                return InvalidAssetId;

            return _idByAsset.TryGetValue(asset, out ushort id) ? id : InvalidAssetId;
        }

        public GenericObjectSo GetAsset(ushort assetId)
        {
            if (assetId == InvalidAssetId || assetId >= _assetsById.Length)
                return null;

            return _assetsById[assetId];
        }
    }
}
