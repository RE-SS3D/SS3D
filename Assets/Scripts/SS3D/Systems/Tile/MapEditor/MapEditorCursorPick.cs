using SS3D.Core;
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

        private static readonly TileLayer[] WallMountLayers = { TileLayer.WallMountHigh, TileLayer.WallMountLow };

        // How far behind the wall collider's surface a mount's mesh may sit and still be picked (the collider is
        // oversized; SteelWall's is 0.35 thick).
        private const float MountBehindHitTolerance = 0.3f;

        public static bool TryPick(
            Camera camera,
            TileMap map,
            MapEditorViewModel viewModel,
            out PlacedTileObject placedTile,
            out PlacedItemObject placedItem)
        {
            placedTile = null;
            placedItem = null;

            if (camera != null && TryRaycast(camera, out Ray ray, out RaycastHit hit))
            {
                placedItem = hit.collider.GetComponentInParent<PlacedItemObject>();
                if (placedItem != null)
                    return true;

                placedTile = hit.collider.GetComponentInParent<PlacedTileObject>();
                if (!IsWallMount(placedTile) && map != null && TryPickWallMount(ray, hit, map, viewModel, out PlacedTileObject mount))
                    placedTile = mount;

                if (placedTile != null)
                    return true;
            }

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

        private static bool TryRaycast(Camera camera, out Ray ray, out RaycastHit hit)
        {
            ray = camera.ScreenPointToRay(Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition);
            return Physics.Raycast(ray, out hit, 500f, StationBounds.WithoutCeiling(Physics.DefaultRaycastLayers));
        }

        private static bool IsWallMount(PlacedTileObject placed) =>
            placed != null && placed.Layer is TileLayer.WallMountHigh or TileLayer.WallMountLow;

        /// <summary>
        /// Wall mounts sit on, or partly inside, the wall's collider, which is bigger than the wall mesh, so the ray
        /// stops at the wall first. Test the renderer bounds of the mounts in the tiles on both sides of the hit
        /// instead, and take the nearest the ray meets before, or just behind, the hit. Only mounts on the hit side of
        /// the collider's middle count, so a mount on the wall's far face never wins over the wall.
        /// </summary>
        private static bool TryPickWallMount(Ray ray, RaycastHit hit, TileMap map, MapEditorViewModel viewModel,
            out PlacedTileObject mount)
        {
            mount = null;
            float nearest = hit.distance + MountBehindHitTolerance;
            Vector3 wallMiddle = hit.collider.bounds.center;
            foreach (Vector3 probe in new[] { hit.point - hit.normal * 0.1f, hit.point + hit.normal * 0.1f })
            {
                if (!map.TryGetTileLocations(probe, out ITileLocation[] locations))
                    continue;

                foreach (TileLayer layer in WallMountLayers)
                {
                    int index = (int)layer;
                    if (index >= locations.Length || locations[index] == null
                        || (viewModel != null && !IsLayerVisible(viewModel, layer)))
                        continue;

                    foreach (PlacedTileObject candidate in locations[index].GetAllPlacedObject())
                    {
                        if (candidate == null)
                            continue;

                        foreach (Renderer renderer in candidate.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer.enabled
                                && Vector3.Dot(renderer.bounds.center - wallMiddle, hit.normal) > 0f
                                && renderer.bounds.IntersectRay(ray, out float distance) && distance <= nearest)
                            {
                                nearest = distance;
                                mount = candidate;
                            }
                        }
                    }
                }
            }

            return mount != null;
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
