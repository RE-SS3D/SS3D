using FishNet;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Underfloor cover visibility: host uses FishNet <c>UnderfloorCoverCondition</c>
    /// (fail → <c>SetRenderersVisible(false)</c>); remotes keep the NetworkObject for
    /// occupancy and use <see cref="Renderer.forceRenderingOff"/> presentationally.
    /// Map Editor authoring keeps underfloor visible.
    /// </summary>
    public static class TileUnderfloorVisibility
    {
        private static readonly TileLayer[] UnderfloorLayers =
        {
            TileLayer.Plenum,
            TileLayer.Wire,
            TileLayer.Disposal,
            TileLayer.PipeLeft,
            TileLayer.PipeMiddle,
            TileLayer.PipeRight,
        };

        private static readonly Vector3[] SurroundingCells =
        {
            new(-1f, 0f, -1f), new(0f, 0f, -1f), new(1f, 0f, -1f),
            new(-1f, 0f, 0f), new(1f, 0f, 0f),
            new(-1f, 0f, 1f), new(0f, 0f, 1f), new(1f, 0f, 1f),
        };

        public static bool IsUnderfloorLayer(TileLayer layer) =>
            layer is TileLayer.Plenum
                or TileLayer.Wire
                or TileLayer.Disposal
                or TileLayer.PipeLeft
                or TileLayer.PipeMiddle
                or TileLayer.PipeRight;

        public static bool IsCoveringTurfGenericType(TileObjectGenericType genericType) =>
            genericType is TileObjectGenericType.Floor
                or TileObjectGenericType.Wall
                or TileObjectGenericType.Door;

        public static bool CellHasCoveringTurf(TileMap map, Vector3 worldPosition)
        {
            if (map == null)
                return false;

            if (!map.TryGetTileLocation(TileLayer.Turf, worldPosition, out ITileLocation location))
                return false;

            if (!location.TryGetPlacedObject(out PlacedTileObject turf) || turf == null)
                return false;

            return IsCoveringTurfGenericType(turf.GenericType);
        }

        /// <summary>
        /// Whether play-mode should hide underfloor MeshRenderers of <paramref name="layer"/> at this cell.
        /// A plenum beside an uncovered cell stays drawn: the gap under the neighbouring floor would
        /// otherwise show space when seen at an angle.
        /// </summary>
        public static bool ShouldHideUnderfloor(TileMap map, Vector3 worldPosition, TileLayer layer, bool mapEditorAuthoring)
        {
            if (mapEditorAuthoring || !CellHasCoveringTurf(map, worldPosition))
                return false;

            if (layer != TileLayer.Plenum)
                return true;

            for (int i = 0; i < SurroundingCells.Length; i++)
            {
                if (!CellHasCoveringTurf(map, worldPosition + SurroundingCells[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Re-apply underfloor visibility for every underfloor object on a cell
        /// (after turf place/clear so cover state stays in sync). Server also rebuilds
        /// FishNet observers so <c>UnderfloorCoverCondition</c> re-evaluates immediately.
        /// Surrounding plenums refresh only when the change flips them, because chunk spawns and
        /// map loads come through here once per floor.
        /// </summary>
        public static void RefreshUnderfloorAt(TileMap map, Vector3 worldPosition)
        {
            if (map == null)
                return;

            for (int i = 0; i < UnderfloorLayers.Length; i++)
            {
                if (!map.TryGetTileLocation(UnderfloorLayers[i], worldPosition, out ITileLocation location))
                    continue;

                if (!location.TryGetPlacedObject(out PlacedTileObject placed) || placed == null)
                    continue;

                RefreshPlaced(placed);
            }

            for (int i = 0; i < SurroundingCells.Length; i++)
            {
                if (map.TryGetTileLocation(TileLayer.Plenum, worldPosition + SurroundingCells[i], out ITileLocation location)
                    && location.TryGetPlacedObject(out PlacedTileObject plenum)
                    && plenum != null
                    && plenum.IsUnderfloorHideOutOfDate)
                {
                    RefreshPlaced(plenum);
                }
            }
        }

        private static void RefreshPlaced(PlacedTileObject placed)
        {
            if (InstanceFinder.IsServer
                && placed.NetworkObject != null
                && placed.NetworkObject.IsSpawned
                && InstanceFinder.ServerManager?.Objects != null)
            {
                InstanceFinder.ServerManager.Objects.RebuildObservers(placed.NetworkObject, timedOnly: false);
            }

            placed.RefreshHostVisibility();
        }

        /// <summary>
        /// Refresh underfloor visibility for every cell occupied by a (multi-tile) turf.
        /// </summary>
        public static void RefreshUnderfloorForTurf(TileMap map, PlacedTileObject turf)
        {
            if (map == null || turf == null)
                return;

            Vector3 origin = new Vector3(turf.WorldOrigin.x, 0f, turf.WorldOrigin.y);
            foreach (Vector2Int gridOffset in turf.GridOffsetList)
            {
                Vector3 cell = origin + new Vector3(gridOffset.x, 0f, gridOffset.y);
                RefreshUnderfloorAt(map, cell);
            }
        }

        /// <summary>
        /// Hide underfloor draw without touching <see cref="Renderer.enabled"/>.
        /// FishNet host AOI owns <c>enabled</c> via <c>SetRenderersVisible</c>; fighting that
        /// flag loses (cover-hide sticks briefly, then AOI re-enables meshes). Use
        /// <see cref="Renderer.forceRenderingOff"/> instead.
        /// </summary>
        public static void SetCoverRenderingHidden(GameObject root, bool hidden)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].forceRenderingOff = hidden;
            }
        }

        /// <summary>Legacy name — prefer <see cref="SetCoverRenderingHidden"/>.</summary>
        public static void DisableChildRenderers(GameObject root) =>
            SetCoverRenderingHidden(root, hidden: true);
    }
}
