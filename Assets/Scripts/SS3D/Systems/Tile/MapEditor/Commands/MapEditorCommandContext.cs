using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using System;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    public sealed class MapEditorCommandContext
    {
        public MapEditorCommandContext(TileMap map, TileResourceLoader loader, ConstructionService construction)
        {
            Map = map;
            Loader = loader;
            Construction = construction;
        }

        public TileMap Map { get; }
        public TileResourceLoader Loader { get; }
        public ConstructionService Construction { get; }
        public MapEditorPlacementMode PlacementMode { get; set; } = MapEditorPlacementMode.Normal;
        public SpawnPointRegistry SpawnPoints { get; set; }
        public GasMarkerRegistry GasMarkers { get; set; }

        /// <summary>Optional override for EditMode tests (bypasses <see cref="Loader"/>).</summary>
        public Func<string, GenericObjectSo> AssetResolver { get; set; }

        /// <summary>Invoked after floor-decal mutations so the host can sync clients.</summary>
        public Action FloorDecalsChanged { get; set; }

        /// <summary>Invoked after spawn-point mutations so the host can refresh editor visuals.</summary>
        public Action SpawnPointsChanged { get; set; }

        public GenericObjectSo ResolveAsset(string assetName)
        {
            if (AssetResolver != null)
                return AssetResolver(assetName);

            return Loader != null ? Loader.GetAsset(assetName) : null;
        }

        public void NotifyFloorDecalsChanged() => FloorDecalsChanged?.Invoke();

        public void NotifySpawnPointsChanged() => SpawnPointsChanged?.Invoke();
    }
}
