using UnityEngine;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Floor paint on a <see cref="TileMap"/>'s chunks (<see cref="FloorPaint"/>). Floors and airlocks take paint; walls,
    /// windows and open space have no squares to show it. Paint stays on the cell when its floor is removed, as floor
    /// decals do, and shows again on the next floor laid there.
    /// </summary>
    public static class FloorPaintMap
    {
        public static bool TryGetFloorPaint(this TileMap map, Vector3 tileWorld, out ushort paint)
        {
            paint = 0;
            TileChunk chunk = map != null ? map.GetChunk(tileWorld) : null;
            if (chunk == null)
                return false;

            Vector2Int local = chunk.GetXY(tileWorld);
            paint = chunk.GetFloorPaint(local.x, local.y);
            return true;
        }

        public static bool TrySetFloorPaint(this TileMap map, Vector3 tileWorld, ushort paint)
        {
            TileChunk chunk = map != null ? map.GetChunk(tileWorld) : null;
            if (chunk == null)
                return false;

            Vector2Int local = chunk.GetXY(tileWorld);
            chunk.SetFloorPaint(local.x, local.y, paint);
            return true;
        }

        /// <summary>
        /// Whether the cell shows floor: a floor, or an airlock, whose two floor halves copy the floor beside them
        /// (<see cref="Connections.AirlockFloorVisual"/>) and so draw paint the same way. Walls and windows show none.
        /// </summary>
        public static bool CanPaint(this TileMap map, Vector3 tileWorld)
        {
            return map != null
                   && map.TryGetTileLocation(TileLayer.Turf, tileWorld, out ITileLocation turf)
                   && turf.TryGetPlacedObject(out PlacedTileObject placed)
                   && placed != null
                   && placed.tileObjectSO != null
                   && placed.tileObjectSO.genericType != TileObjectGenericType.Wall;
        }
    }
}
