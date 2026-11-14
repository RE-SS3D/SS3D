using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Server-authoritative tile and construction-item mutations.
    /// </summary>
    public interface IConstructionService
    {
        /// <param name="setup">Saved setup applied right after the spawn (undo, Dropper copy); null for prefab defaults.</param>
        PlaceResult TryPlaceTile(TileObjectSo tileObject, Vector3 worldPosition, Direction direction, bool replaceExisting,
            bool skipBuildCheck = false, IReadOnlyList<SavedSetupEntry> setup = null);

        /// <param name="setup">Saved setup applied right after the spawn (undo, Dropper copy); null for prefab defaults.</param>
        PlaceResult TryPlaceItem(ItemObjectSo itemObject, Vector3 worldPosition, Quaternion rotation, GameObject existingItem = null,
            IReadOnlyList<SavedSetupEntry> setup = null);

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

        /// <summary>For a cleared item, its setup read before it was destroyed; null otherwise.</summary>
        public SavedSetupEntry[] Setup { get; init; }

        public static ClearResult Failed => new() { Success = false };
    }

    public readonly struct PreviewResult
    {
        public bool CanBuild { get; init; }
        public BuildFailReason[] Failures { get; init; }

        public static PreviewResult Ok => new()
        {
            CanBuild = true,
            Failures = System.Array.Empty<BuildFailReason>(),
        };

        public static PreviewResult Failed(params BuildFailReason[] failures) =>
            new()
            {
                CanBuild = false,
                Failures = failures ?? System.Array.Empty<BuildFailReason>(),
            };

        public string PrimaryMessage => BuildFailMessages.FormatPrimary(Failures);
    }

    public readonly struct SpawnResult
    {
        public bool Success { get; init; }
        public GameObject Instance { get; init; }

        public static SpawnResult Failed => new() { Success = false };
    }
}
