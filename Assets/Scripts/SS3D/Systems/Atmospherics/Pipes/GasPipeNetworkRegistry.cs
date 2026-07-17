using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Server-side storage for gas pipe network topology and pooled contents.
    /// </summary>
    public sealed class GasPipeNetworkRegistry
    {
        private readonly Dictionary<GasPipeSegmentKey, GasPipeNetworkId> _segmentNetworks = new();
        private readonly Dictionary<GasPipeNetworkId, GasPipeNetworkRecord> _networks = new();
        private ushort _nextNetworkId = GasPipeNetworkId.NoneValue + 1;
        private readonly int _gasTypeCount;
        private int _topologyVersion;

        public GasPipeNetworkRegistry(int gasTypeCount)
        {
            _gasTypeCount = gasTypeCount;
        }

        public IReadOnlyDictionary<GasPipeNetworkId, GasPipeNetworkRecord> Networks => _networks;

        public int NetworkCount => _networks.Count;

        /// <summary>
        /// Incremented whenever network topology is rebuilt. Ports cache resolutions against this.
        /// </summary>
        public int TopologyVersion => _topologyVersion;

        public bool TryGetNetwork(GasPipeNetworkId id, out GasPipeNetworkRecord record) =>
            _networks.TryGetValue(id, out record);

        public bool TryGetNetworkForSegment(GasPipeSegmentKey key, out GasPipeNetworkId id, out GasPipeNetworkRecord record)
        {
            if (_segmentNetworks.TryGetValue(key, out id) && _networks.TryGetValue(id, out record))
                return true;

            id = GasPipeNetworkId.None;
            record = null;
            return false;
        }

        public void RebuildAll(TileMap map)
        {
            Clear();

            if (map == null)
                return;

            var assigned = new HashSet<GasPipeSegmentKey>();
            foreach (PlacedTileObject segment in AtmosPipeConnectivity.EnumerateAllGasPipeSegments(map))
            {
                GasPipeSegmentKey key = GasPipeSegmentKey.From(segment);
                if (!assigned.Add(key))
                    continue;

                CreateNetworkFromSeed(map, segment, assigned);
            }

            _topologyVersion++;
        }

        public void RebuildAround(TileMap map, TileCoord coord)
        {
            if (map == null)
                return;

            var segmentsToReprocess = new HashSet<GasPipeSegmentKey>();
            var networkIdsToClear = new HashSet<GasPipeNetworkId>();

            foreach (TileCoord affectedCoord in AtmosPipeConnectivity.EnumerateCoordsAround(map, coord))
            {
                foreach (TileLayer layer in AtmosPipeConnectivity.GasPipeLayers)
                {
                    if (!AtmosPipeConnectivity.TryGetSegment(map, affectedCoord, layer, out PlacedTileObject segment))
                        continue;

                    GasPipeSegmentKey key = GasPipeSegmentKey.From(segment);
                    segmentsToReprocess.Add(key);

                    if (_segmentNetworks.TryGetValue(key, out GasPipeNetworkId networkId))
                        networkIdsToClear.Add(networkId);
                }
            }

            foreach (GasPipeNetworkId networkId in networkIdsToClear)
            {
                if (!_networks.TryGetValue(networkId, out GasPipeNetworkRecord record))
                    continue;

                foreach (GasPipeSegmentKey segmentKey in record.Segments)
                    segmentsToReprocess.Add(segmentKey);
            }

            foreach (GasPipeNetworkId networkId in networkIdsToClear)
                RemoveNetwork(networkId);

            var assigned = new HashSet<GasPipeSegmentKey>(_segmentNetworks.Keys);
            foreach (GasPipeSegmentKey key in segmentsToReprocess)
            {
                if (assigned.Contains(key))
                    continue;

                if (!AtmosPipeConnectivity.TryGetSegment(map, key.Coord, key.Layer, out PlacedTileObject segment))
                    continue;

                CreateNetworkFromSeed(map, segment, assigned);
            }

            _topologyVersion++;
        }

        private void CreateNetworkFromSeed(TileMap map, PlacedTileObject seed, HashSet<GasPipeSegmentKey> assigned)
        {
            HashSet<GasPipeSegmentKey> segments = AtmosPipeConnectivity.CollectNetworkSegments(map, seed);
            if (segments.Count == 0)
                return;

            var networkId = new GasPipeNetworkId(_nextNetworkId++);
            var record = new GasPipeNetworkRecord(networkId, _gasTypeCount);

            foreach (GasPipeSegmentKey segmentKey in segments)
            {
                record.Segments.Add(segmentKey);
                _segmentNetworks[segmentKey] = networkId;
                assigned.Add(segmentKey);
            }

            record.RecalculateVolume();
            _networks[networkId] = record;
        }

        private void RemoveNetwork(GasPipeNetworkId networkId)
        {
            if (!_networks.TryGetValue(networkId, out GasPipeNetworkRecord record))
                return;

            foreach (GasPipeSegmentKey segmentKey in record.Segments)
                _segmentNetworks.Remove(segmentKey);

            _networks.Remove(networkId);
        }

        private void Clear()
        {
            _segmentNetworks.Clear();
            _networks.Clear();
            _nextNetworkId = GasPipeNetworkId.NoneValue + 1;
        }
    }
}
