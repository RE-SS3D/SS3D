using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Session-only debris identity. No colliders, tile occupancy or salvage inventory.</summary>
    public struct SteelWreckageRecord
    {
        public TileCoord Coord;
        public TileLayer Layer;
        public byte Kind; // 1 wall, 2 table
        public Vector3 Position;
        public Vector3 Size;
        public float Yaw;
        public int Seed;

        public static SteelWreckageRecord Capture(PlacedTileObject placed, TileCoord coord)
        {
            bool table = StructuralIntegrityRules.IsSteelTable(placed.tileObjectSO);
            bool wall = placed.GenericType == TileObjectGenericType.Wall
                && !TileOccupancyEvaluator.IsWindowName(placed.tileObjectSO.NameString)
                && placed.tileObjectSO.specificType == TileObjectSpecificType.Steel;
            if (!table && !wall) return default;
            Bounds bounds = new(placed.transform.position, Vector3.zero);
            bool found = false;
            foreach (MeshRenderer renderer in placed.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || !SteelWallDamageVfx.IsDamageSurface(renderer, placed)) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return new SteelWreckageRecord
            {
                Coord = coord, Layer = placed.Layer, Kind = (byte)(table ? 2 : 1),
                Position = new Vector3(bounds.center.x, found ? bounds.min.y + 0.025f : placed.transform.position.y, bounds.center.z),
                Size = found ? bounds.size : new Vector3(1f, table ? 0.8f : 2f, 1f),
                Yaw = placed.transform.eulerAngles.y,
                Seed = unchecked(coord.Grid.x * 73856093 ^ coord.Grid.y * 19349663 ^ coord.MapId * 83492791 ^ (table ? 7919 : 0))
            };
        }
    }
}
