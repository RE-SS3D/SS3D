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
            TileCoord deviceCoord,
            out GasPipeNetworkId networkId,
            out GasPipeSegmentKey segmentKey)
        {
            networkId = GasPipeNetworkId.None;
            segmentKey = default;

            if (map == null || registry == null)
                return false;

            foreach (TileLayer layer in AtmosPipeConnectivity.GasPipeLayers)
            {
                if (!AtmosPipeConnectivity.TryGetSegment(map, deviceCoord, layer, out PlacedTileObject segment))
                    continue;

                segmentKey = GasPipeSegmentKey.From(segment);
                if (registry.TryGetNetworkForSegment(segmentKey, out networkId, out _))
                    return true;
            }

            return false;
        }
    }
}
