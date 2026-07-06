using FishNet;
using SS3D.Data.AssetDatabases;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Sole server-side entry point for tilemap placement, clearing, preview checks, and craft spawns.
    /// </summary>
    public sealed class ConstructionService : IConstructionService
    {
        private readonly TileMap _map;
        private readonly ITileQueryService _query;

        public ConstructionService(TileMap map, ITileQueryService query)
        {
            _map = map;
            _query = query;
        }

        public PlaceResult TryPlaceTile(TileObjectSo tileObject, Vector3 worldPosition, Direction direction, bool replaceExisting)
        {
            if (_map == null || tileObject == null)
                return PlaceResult.Failed;

            Vector3 gridPosition = TileHelper.GetClosestPosition(worldPosition);
            if (!_map.PlaceTileObject(tileObject, gridPosition, direction, skipBuildCheck: false, replaceExisting,
                    skipAdjacency: false, out GameObject instance))
            {
                return PlaceResult.Failed;
            }

            return new PlaceResult { Success = true, Instance = instance };
        }

        public PlaceResult TryPlaceItem(ItemObjectSo itemObject, Vector3 worldPosition, Quaternion rotation, GameObject existingItem = null)
        {
            if (_map == null || itemObject == null)
                return PlaceResult.Failed;

            _map.PlaceItemObject(worldPosition, rotation, itemObject, existingItem);
            return new PlaceResult { Success = true, Instance = existingItem };
        }

        public ClearResult TryClearTile(Vector3 worldPosition, TileLayer layer, Direction direction)
        {
            if (_map == null)
                return ClearResult.Failed;

            _map.ClearTileObject(worldPosition, layer, direction);
            return new ClearResult { Success = true };
        }

        public ClearResult TryClearItem(Vector3 worldPosition, ItemObjectSo itemObject)
        {
            if (_map == null || itemObject == null)
                return ClearResult.Failed;

            _map.ClearItemObject(worldPosition, itemObject);
            return new ClearResult { Success = true };
        }

        public PreviewResult TryPreviewTile(TileObjectSo tileObject, Vector3 worldPosition, Direction direction, bool replaceExisting)
        {
            if (_map == null || tileObject == null)
                return new PreviewResult { CanBuild = false };

            return new PreviewResult
            {
                CanBuild = _map.CanBuild(tileObject, worldPosition, direction, replaceExisting),
            };
        }

        public SpawnResult SpawnIngredient(GameObject prefab, Vector3 nearWorldPosition, Direction direction = Direction.North,
            TileLayer freeTileLayer = TileLayer.Turf)
        {
            if (prefab == null)
                return SpawnResult.Failed;

            if (prefab.TryGetComponent(out PlacedTileObject tileTemplate))
            {
                PlaceResult placed = TryPlaceTile(
                    tileTemplate.tileObjectSO,
                    TileHelper.GetClosestPosition(nearWorldPosition),
                    direction,
                    replaceExisting: false);

                return new SpawnResult { Success = placed.Success, Instance = placed.Instance };
            }

            Vector3 spawnPosition = ResolveSpawnPosition(nearWorldPosition, freeTileLayer);
            Quaternion rotation = Quaternion.Euler(0f, TileHelper.GetRotationAngle(direction), 0f);
            GameObject instance = Object.Instantiate(prefab, spawnPosition, rotation);
            InstanceFinder.ServerManager.Spawn(instance);
            instance.SetActive(true);

            return new SpawnResult { Success = true, Instance = instance };
        }

        public bool TryFindFreeAdjacentTile(TileCoord near, TileLayer layer, out TileCoord free)
        {
            if (_query != null && _query.TryFindFreeTile(near, layer, out free))
                return true;

            free = default;
            return false;
        }

        private Vector3 ResolveSpawnPosition(Vector3 nearWorldPosition, TileLayer freeTileLayer)
        {
            if (_query == null)
                return nearWorldPosition;

            TileCoord near = _query.WorldToTile(nearWorldPosition, _map.MapId);
            if (TryFindFreeAdjacentTile(near, freeTileLayer, out TileCoord free))
                return _query.TileToWorld(free);

            return nearWorldPosition;
        }
    }
}
