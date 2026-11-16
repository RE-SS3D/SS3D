using SS3D.Systems.Tile;
using System.Collections.Generic;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Server-side storage for disposal pipe network topology, mirroring <c>GasPipeNetworkRegistry</c>.
    /// </summary>
    public sealed class DisposalNetworkRegistry
    {
        private readonly Dictionary<DisposalSegmentKey, DisposalNetworkId> _segmentNetworks = new();
        private readonly Dictionary<DisposalNetworkId, DisposalNetworkRecord> _networks = new();
        private ushort _nextNetworkId = DisposalNetworkId.NoneValue + 1;

        public IReadOnlyDictionary<DisposalNetworkId, DisposalNetworkRecord> Networks => _networks;

        public int NetworkCount => _networks.Count;

        public bool TryGetNetwork(DisposalNetworkId id, out DisposalNetworkRecord record) =>
            _networks.TryGetValue(id, out record);

        public bool TryGetNetworkForSegment(DisposalSegmentKey key, out DisposalNetworkId id, out DisposalNetworkRecord record)
        {
            if (_segmentNetworks.TryGetValue(key, out id) && _networks.TryGetValue(id, out record))
                return true;

            id = DisposalNetworkId.None;
            record = null;
            return false;
        }

        public void RebuildAll(TileMap map)
        {
            Clear();

            if (map == null)
                return;

            HashSet<DisposalSegmentKey> assigned = new();
            foreach (PlacedTileObject segment in DisposalPipeConnectivity.EnumerateAllDisposalPipeSegments(map))
            {
                DisposalSegmentKey key = DisposalSegmentKey.From(segment);
                if (!assigned.Add(key))
                    continue;

                CreateNetworkFromSeed(map, segment, assigned);
            }
        }

        public void RebuildAround(TileMap map, TileCoord coord)
        {
            if (map == null)
                return;

            HashSet<DisposalSegmentKey> segmentsToReprocess = new();
            HashSet<DisposalNetworkId> networkIdsToClear = new();

            foreach (TileCoord affectedCoord in DisposalPipeConnectivity.EnumerateCoordsAround(map, coord))
            {
                if (!DisposalPipeConnectivity.TryGetSegment(map, affectedCoord, out PlacedTileObject segment))
                    continue;

                DisposalSegmentKey key = DisposalSegmentKey.From(segment);
                segmentsToReprocess.Add(key);

                if (_segmentNetworks.TryGetValue(key, out DisposalNetworkId networkId))
                    networkIdsToClear.Add(networkId);
            }

            foreach (DisposalNetworkId networkId in networkIdsToClear)
            {
                if (!_networks.TryGetValue(networkId, out DisposalNetworkRecord record))
                    continue;

                foreach (DisposalSegmentKey segmentKey in record.Segments)
                    segmentsToReprocess.Add(segmentKey);
            }

            foreach (DisposalNetworkId networkId in networkIdsToClear)
                RemoveNetwork(networkId);

            HashSet<DisposalSegmentKey> assigned = new(_segmentNetworks.Keys);
            foreach (DisposalSegmentKey key in segmentsToReprocess)
            {
                if (assigned.Contains(key))
                    continue;

                if (!DisposalPipeConnectivity.TryGetSegment(map, key.Coord, out PlacedTileObject segment))
                    continue;

                CreateNetworkFromSeed(map, segment, assigned);
            }
        }

        private void CreateNetworkFromSeed(TileMap map, PlacedTileObject seed, HashSet<DisposalSegmentKey> assigned)
        {
            DisposalNetworkWalkResult walk = DisposalPipeConnectivity.CollectNetwork(map, seed);
            if (walk.Segments.Count == 0)
                return;

            DisposalNetworkId networkId = new(_nextNetworkId++);
            DisposalNetworkRecord record = new(networkId);

            foreach (DisposalSegmentKey segmentKey in walk.Segments)
            {
                record.Segments.Add(segmentKey);
                _segmentNetworks[segmentKey] = networkId;
                assigned.Add(segmentKey);
            }

            record.Terminals.AddRange(walk.Terminals);
            _networks[networkId] = record;
        }

        /// <summary>
        /// Loses any capsules still riding the removed network's segments — callers must relocate them
        /// (spill at the last known segment) before rebuilding, mirroring the pipe-cut sabotage rule (§7).
        /// </summary>
        private void RemoveNetwork(DisposalNetworkId networkId)
        {
            if (!_networks.TryGetValue(networkId, out DisposalNetworkRecord record))
                return;

            foreach (DisposalSegmentKey segmentKey in record.Segments)
                _segmentNetworks.Remove(segmentKey);

            _networks.Remove(networkId);
        }

        private void Clear()
        {
            _segmentNetworks.Clear();
            _networks.Clear();
            _nextNetworkId = DisposalNetworkId.NoneValue + 1;
        }
    }
}
