namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Server: which room a device is in and whether that room has power, for a view whose analyzer marks devices in a
    /// room with no power (the Atmos view, Phase 4). The electricity side answers it from the same room → APC knowledge
    /// the Power view tints and hatches rooms by, so neither domain reads the other's internals.
    /// </summary>
    public interface ISystemsViewRooms
    {
        /// <summary>Moves whenever an answer may have changed: rooms repainted, an APC placed, removed, broken or repaired.</summary>
        int Version { get; }

        /// <summary>The room <paramref name="device"/> is in (the tile in front of a wall mount); false outside every room.</summary>
        bool TryGetRoom(PlacedTileObject device, out ushort areaId, out string name);

        /// <summary>The room has a working APC: one is there and it isn't broken (the Power view doesn't hatch it).</summary>
        bool HasPower(ushort areaId);
    }
}
