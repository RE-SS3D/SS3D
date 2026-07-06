using SS3D.Data.AssetDatabases;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Server-authoritative tile and construction-item mutations.
    /// </summary>
    public interface IConstructionService
    {
        PlaceResult TryPlaceTile(TileObjectSo tileObject, Vector3 worldPosition, Direction direction, bool replaceExisting);

        PlaceResult TryPlaceItem(ItemObjectSo itemObject, Vector3 worldPosition, Quaternion rotation, GameObject existingItem = null);

        ClearResult TryClearTile(Vector3 worldPosition, TileLayer layer, Direction direction);

        ClearResult TryClearItem(Vector3 worldPosition, ItemObjectSo itemObject);

        PreviewResult TryPreviewTile(TileObjectSo tileObject, Vector3 worldPosition, Direction direction, bool replaceExisting);

        SpawnResult SpawnIngredient(GameObject prefab, Vector3 nearWorldPosition, Direction direction = Direction.North,
            TileLayer freeTileLayer = TileLayer.Turf);

        bool TryFindFreeAdjacentTile(TileCoord near, TileLayer layer, out TileCoord free);
    }

    public readonly struct PlaceResult
    {
        public bool Success { get; init; }
        public GameObject Instance { get; init; }

        public static PlaceResult Failed => new() { Success = false };
    }

    public readonly struct ClearResult
    {
        public bool Success { get; init; }

        public static ClearResult Failed => new() { Success = false };
    }

    public readonly struct PreviewResult
    {
        public bool CanBuild { get; init; }
    }

    public readonly struct SpawnResult
    {
        public bool Success { get; init; }
        public GameObject Instance { get; init; }

        public static SpawnResult Failed => new() { Success = false };
    }
}
