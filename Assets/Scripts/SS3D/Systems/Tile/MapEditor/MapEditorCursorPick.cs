using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Resolves which placed object is under the map-editor cursor for Select / Dropper / Move.
    /// </summary>
    public static class MapEditorCursorPick
    {
        /// <summary>
        /// Visual / authoring priority: foreground first, plenum last.
        /// <see cref="TileMap.TryGetTileLocations"/> arrays are enum-ordered (Plenum=0), so naive
        /// foreach always samples the plenum under every tile.
        /// </summary>
        private static readonly TileLayer[] LayerPickOrder =
        {
            TileLayer.FurnitureTop,
            TileLayer.FurnitureBase,
            TileLayer.WallMountHigh,
            TileLayer.WallMountLow,
            TileLayer.PipeManifold,
            TileLayer.PipeSurface,
            TileLayer.PipeMiddle,
            TileLayer.PipeLeft,
            TileLayer.PipeRight,
            TileLayer.Turf,
            TileLayer.Disposal,
            TileLayer.Wire,
            TileLayer.Plenum,
        };

        public static bool TryPick(
            Camera camera,
            TileMap map,
            MapEditorViewModel viewModel,
            out PlacedTileObject placedTile,
            out PlacedItemObject placedItem)
        {
            placedTile = null;
            placedItem = null;

            if (camera != null && TryRaycastItem(camera, out placedItem))
                return true;

            if (camera != null && TryRaycastTile(camera, out placedTile))
                return true;

            if (map == null)
                return false;

            Vector3 position = TileHelper.GetPointedPosition(true, camera);
            if (!map.TryGetTileLocations(position, out ITileLocation[] locations))
                return false;

            // Prefer visible layer groups when the user has dimmed plenums/etc.
            if (TryPickFromLayers(locations, viewModel, requireVisible: true, out placedTile))
                return true;

            return TryPickFromLayers(locations, viewModel, requireVisible: false, out placedTile);
        }

        private static bool TryRaycastItem(Camera camera, out PlacedItemObject item)
        {
            item = null;
            if (!TryRaycast(camera, out RaycastHit hit))
                return false;

            item = hit.collider.GetComponentInParent<PlacedItemObject>();
            return item != null;
        }

        private static bool TryRaycastTile(Camera camera, out PlacedTileObject placed)
        {
            placed = null;
            if (!TryRaycast(camera, out RaycastHit hit))
                return false;

            placed = hit.collider.GetComponentInParent<PlacedTileObject>();
            return placed != null;
        }

        private static bool TryRaycast(Camera camera, out RaycastHit hit)
        {
            Ray ray = camera.ScreenPointToRay(Mouse.current != null
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition);
            return Physics.Raycast(ray, out hit, 500f);
        }

        private static bool TryPickFromLayers(
            ITileLocation[] locations,
            MapEditorViewModel viewModel,
            bool requireVisible,
            out PlacedTileObject placed)
        {
            placed = null;
            foreach (TileLayer layer in LayerPickOrder)
            {
                if (requireVisible && viewModel != null && !IsLayerVisible(viewModel, layer))
                    continue;

                int index = (int)layer;
                if (index < 0 || index >= locations.Length)
                    continue;

                ITileLocation location = locations[index];
                if (location == null || location.IsFullyEmpty())
                    continue;

                foreach (PlacedTileObject candidate in location.GetAllPlacedObject())
                {
                    if (candidate == null)
                        continue;

                    placed = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool IsLayerVisible(MapEditorViewModel viewModel, TileLayer layer)
        {
            if (!TileLayerCategoryMapping.TryGetCategoryForLayer(layer, out TileLayerCategory category))
                return true;

            return viewModel.IsLayerCategoryVisible(category);
        }
    }
}
