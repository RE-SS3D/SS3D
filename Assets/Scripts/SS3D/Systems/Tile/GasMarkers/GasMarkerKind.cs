namespace SS3D.Systems.Tile.GasMarkers
{
    public enum GasMarkerKind
    {
        /// <summary>Holds its tile at the marker's mix every tick, so a sealed room never runs out.</summary>
        Source,

        /// <summary>Fills the pipe network on one line of its tile, once at round start.</summary>
        LineFill,

        /// <summary>Gives the room around it the marker's mix, once at round start. No gas means vacuum.</summary>
        RoomAir,
    }
}
