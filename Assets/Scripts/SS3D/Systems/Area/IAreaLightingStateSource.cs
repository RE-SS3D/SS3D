using SS3D.Systems.Tile;

namespace SS3D.Systems.Area
{
    public interface IAreaLightingStateSource
    {
        bool TryGetLightingState(AreaId areaId, out AreaLightingState state);

        bool TryGetLightingStateForTile(TileCoord coord, out AreaLightingState state);
    }
}
