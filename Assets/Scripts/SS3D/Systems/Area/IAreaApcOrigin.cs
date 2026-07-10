using SS3D.Systems.Tile;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// APC contract used by the area subsystem without referencing machine-interface types.
    /// </summary>
    public interface IAreaApcOrigin
    {
        TileCoord OriginTile { get; }

        Direction FacingDirection { get; }

        string DisplayName { get; }

        void SetMultipleApcsInArea(bool value);
    }
}
