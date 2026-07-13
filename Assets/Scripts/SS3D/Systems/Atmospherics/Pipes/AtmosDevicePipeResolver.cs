using SS3D.Systems.Tile;
using System.Collections.Generic;

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

            foreach (TileCoord coord in AtmosPipeConnectivity.EnumerateCoordsAround(map, deviceCoord))
            {
                if (TryResolveNetworkAt(map, registry, coord, out networkId, out segmentKey))
                    return true;
            }

            return false;
        }

        private static bool TryResolveNetworkAt(
            TileMap map,
            GasPipeNetworkRegistry registry,
            TileCoord coord,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            foreach (TileLayer layer in AtmosPipeConnectivity.GasPipeLayers)
            {
                if (!AtmosPipeConnectivity.TryGetSegment(map, coord, layer, out PlacedTileObject segment))
                    continue;

                segmentKey = GasPipeSegmentKey.From(segment);
                if (registry.TryGetNetworkForSegment(segmentKey, out networkId, out _))
                    return true;
            }

            return false;
        }
    }
}
