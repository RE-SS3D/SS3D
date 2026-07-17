using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Resolves gas pipe networks connected to atmos furniture at a grid coordinate.
    /// </summary>
    public static class AtmosDevicePipeResolver
    {
        public static bool TryResolveNetwork(
            TileMap map,
            GasPipeNetworkRegistry registry,
            PlacedTileObject device,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            if (device == null)
                return false;

            return TryResolveNetwork(
                map,
                registry,
                new TileCoord(device.MapId, device.WorldOrigin),
                out networkId,
                out segmentKey);
        }

        public static bool TryResolveNetwork(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord deviceCoord,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            if (map == null || registry == null)
                return false;

            // Unrolled neighbour walk — avoids EnumerateCoordsAround iterator allocation.
            if (TryResolveNetworkAt(map, registry, deviceCoord, out networkId, out segmentKey))
                return true;

            if (TryResolveNetworkAt(map, registry, Offset(deviceCoord, 0, 1), out networkId, out segmentKey))
                return true;
            if (TryResolveNetworkAt(map, registry, Offset(deviceCoord, 1, 0), out networkId, out segmentKey))
                return true;
            if (TryResolveNetworkAt(map, registry, Offset(deviceCoord, 0, -1), out networkId, out segmentKey))
                return true;
            if (TryResolveNetworkAt(map, registry, Offset(deviceCoord, -1, 0), out networkId, out segmentKey))
                return true;

            return false;
        }

        private static TileCoord Offset(TileCoord coord, int dx, int dy) =>
            new TileCoord(coord.MapId, coord.Grid.x + dx, coord.Grid.y + dy);

        private static bool TryResolveNetworkAt(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord coord,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            TileLayer[] layers = AtmosPipeConnectivity.GasPipeLayers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (!AtmosPipeConnectivity.TryGetSegment(map, coord, layers[i], out PlacedTileObject segment))
                    continue;

                segmentKey = GasPipeSegmentKey.From(segment);
                if (registry.TryGetNetworkForSegment(segmentKey, out networkId, out _))
                    return true;
            }

            return false;
        }
    }
}
