using System.Collections.Generic;
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
            return TryPickFromLocations(locations, viewModel, out placedTile);
        }

        /// <summary>
        /// The tile under the cursor on the plane at <paramref name="height"/>. A systems view draws on top of
        /// everything at its own height, so this is the tile whose run the user sees under the cursor, even through a
        /// wall that the raycast would stop at.
        /// </summary>
        public static bool TryGetPointedTile(Camera camera, float height, out Vector2Int tile)
        {
            tile = default;
            if (camera == null || !new Plane(Vector3.up, new Vector3(0f, height, 0f)).Raycast(PointerRay(camera), out float distance))
                return false;

            Vector3 point = PointerRay(camera).GetPoint(distance);
            tile = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z));
            return true;
        }

        /// <summary>
        /// The layers a systems view trace tries on a pointed tile: the view's own layer category first (a pipe under a
        /// table), then the usual pick order.
        /// </summary>
        public static IEnumerable<TileLayer> TraceLayerOrder(TileLayerCategory? ownLayer)
        {
            IReadOnlyList<TileLayer> own = ownLayer.HasValue ? TileLayerCategoryMapping.GetLayers(ownLayer.Value) : System.Array.Empty<TileLayer>();
            foreach (TileLayer layer in LayerPickOrder)
            {
                if (Contains(own, layer))
                    yield return layer;
            }

            foreach (TileLayer layer in own)
            {
                if (System.Array.IndexOf(LayerPickOrder, layer) < 0)
                    yield return layer;
            }

            foreach (TileLayer layer in LayerPickOrder)
            {
                if (!Contains(own, layer))
                    yield return layer;
            }
        }

        private static bool Contains(IReadOnlyList<TileLayer> layers, TileLayer layer)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] == layer)
                    return true;
            }

            return false;
        }

        private static Ray PointerRay(Camera camera) => camera.ScreenPointToRay(Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition);

        private static bool TryRaycast(Camera camera, out Ray ray, out RaycastHit hit)
        {
            ray = PointerRay(camera);
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

        /// <summary>
        /// The tile-grid half of <see cref="TryPick"/>: the first object in <see cref="LayerPickOrder"/> on these
        /// locations, preferring pickable layers (<see cref="MapEditorViewModel.IsLayerPickable"/>).
        /// </summary>
        public static bool TryPickFromLocations(ITileLocation[] locations, MapEditorViewModel viewModel, out PlacedTileObject placed) =>
            TryPickFromLayers(locations, viewModel, requireVisible: true, out placed)
            || TryPickFromLayers(locations, viewModel, requireVisible: false, out placed);

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

        // Pickable, not merely visible: a systems view dims its own layer but keeps it first to pick.
        private static bool IsLayerVisible(MapEditorViewModel viewModel, TileLayer layer)
        {
            if (!TileLayerCategoryMapping.TryGetCategoryForLayer(layer, out TileLayerCategory category))
                return true;

            return viewModel.IsLayerPickable(category);
        }
    }
}
