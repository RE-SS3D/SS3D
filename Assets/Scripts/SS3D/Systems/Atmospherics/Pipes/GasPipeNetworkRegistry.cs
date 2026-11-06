using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using UnityEngine;

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
        private readonly List<PipeGasRelease> _pendingReleases = new();
        private readonly int _gasTypeCount;
        private readonly float[] _specificHeats;
        private int _topologyVersion;

        public GasPipeNetworkRegistry(int gasTypeCount, float[] specificHeats = null)
        {
            _gasTypeCount = gasTypeCount;
            _specificHeats = specificHeats ?? DefaultSpecificHeats(gasTypeCount);
        }

        public IReadOnlyDictionary<GasPipeNetworkId, GasPipeNetworkRecord> Networks => _networks;

        public int NetworkCount => _networks.Count;

        /// <summary>
        /// Incremented whenever network topology is rebuilt. Ports cache resolutions against this.
        /// </summary>
        public int TopologyVersion => _topologyVersion;

        /// <summary>
        /// Gas from removed segments, waiting for <see cref="AtmosPipeSimulation"/> to put it on its tiles.
        /// </summary>
        public IReadOnlyList<PipeGasRelease> PendingReleases => _pendingReleases;

        public void ClearPendingReleases() => _pendingReleases.Clear();

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

        /// <summary>
        /// Replaces the contents of the network holding <paramref name="segment"/> with a mix at this pressure.
        /// </summary>
        public bool TryFillNetwork(GasPipeSegmentKey segment, float[] gasFractions, float pressureKpa)
        {
            if (!TryGetNetworkForSegment(segment, out _, out GasPipeNetworkRecord record))
                return false;

            float moles = AtmosPipeThermo.GetMolesForPressure(pressureKpa, record.Temperature, record.Volume);
            AtmosGasMix.FillMoles(gasFractions, moles, record.Moles);
            return true;
        }

        /// <summary>
        /// Moves up to <paramref name="moles"/> of a network's mixture into another, carrying its heat.
        /// </summary>
        /// <returns>The moles moved, which is less than asked when the source runs out.</returns>
        public float MoveGas(GasPipeNetworkRecord source, GasPipeNetworkRecord target, float moles)
        {
            float available = source.GetTotalMoles(_gasTypeCount);
            if (moles <= 0f || available <= 0f || source == target)
                return 0f;

            float moved = Mathf.Min(moles, available);
            float fraction = moved / available;
            AddGas(target, source, fraction);

            for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                source.Moles[gasId] -= source.Moles[gasId] * fraction;

            return moved;
        }

        /// <summary>
        /// Moves a share of each gas in <paramref name="gasMask"/> into another network, carrying its
        /// heat. Bit n is gas id n.
        /// </summary>
        /// <returns>The moles moved.</returns>
        public float MoveGases(GasPipeNetworkRecord source, GasPipeNetworkRecord target, ulong gasMask, float fraction)
        {
            if (fraction <= 0f || source == target)
                return 0f;

            fraction = Mathf.Min(fraction, 1f);
            float targetCapacity = target.GetHeatCapacity(_specificHeats, _gasTypeCount);
            float addedCapacity = 0f;
            float moved = 0f;

            for (int gasId = 0; gasId < _gasTypeCount; gasId++)
            {
                if ((gasMask & (1UL << gasId)) == 0UL)
                    continue;

                float share = source.Moles[gasId] * fraction;
                addedCapacity += share * _specificHeats[gasId];
                target.Moles[gasId] += share;
                source.Moles[gasId] -= share;
                moved += share;
            }

            if (moved > 0f)
            {
                target.Temperature = AtmosPipeThermo.BlendTemperature(
                    target.Temperature,
                    targetCapacity,
                    source.Temperature,
                    addedCapacity);
            }

            return moved;
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

            // A removed segment with no neighbours left can only be found through its key.
            foreach (KeyValuePair<GasPipeSegmentKey, GasPipeNetworkId> pair in _segmentNetworks)
            {
                if (pair.Key.Coord.Equals(coord))
                    networkIdsToClear.Add(pair.Value);
            }

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

            var previousNetworks = new List<GasPipeNetworkRecord>(networkIdsToClear.Count);
            foreach (GasPipeNetworkId networkId in networkIdsToClear)
            {
                if (!_networks.TryGetValue(networkId, out GasPipeNetworkRecord record))
                    continue;

                previousNetworks.Add(record);
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

            foreach (GasPipeNetworkRecord previous in previousNetworks)
                RedistributeGas(previous);

            _topologyVersion++;
        }

        /// <summary>
        /// Hands a replaced network's gas on by segment count: each surviving segment carries its
        /// share into its new network, and each removed one releases its share on its tile.
        /// </summary>
        private void RedistributeGas(GasPipeNetworkRecord previous)
        {
            if (previous.GetTotalMoles(_gasTypeCount) <= 0f)
                return;

            var survivingSegments = new Dictionary<GasPipeNetworkRecord, int>();
            float share = 1f / previous.Segments.Count;

            foreach (GasPipeSegmentKey key in previous.Segments)
            {
                if (TryGetNetworkForSegment(key, out _, out GasPipeNetworkRecord record))
                {
                    survivingSegments.TryGetValue(record, out int count);
                    survivingSegments[record] = count + 1;
                    continue;
                }

                var released = new float[_gasTypeCount];
                for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                    released[gasId] = previous.Moles[gasId] * share;

                _pendingReleases.Add(new PipeGasRelease(key.Coord, released, previous.Temperature));
            }

            foreach (KeyValuePair<GasPipeNetworkRecord, int> pair in survivingSegments)
                AddGas(pair.Key, previous, pair.Value * share);
        }

        private void AddGas(GasPipeNetworkRecord target, GasPipeNetworkRecord source, float fraction)
        {
            float targetCapacity = target.GetHeatCapacity(_specificHeats, _gasTypeCount);
            float addedCapacity = source.GetHeatCapacity(_specificHeats, _gasTypeCount) * fraction;

            for (int gasId = 0; gasId < _gasTypeCount; gasId++)
                target.Moles[gasId] += source.Moles[gasId] * fraction;

            target.Temperature = AtmosPipeThermo.BlendTemperature(
                target.Temperature,
                targetCapacity,
                source.Temperature,
                addedCapacity);
        }

        private static float[] DefaultSpecificHeats(int gasTypeCount)
        {
            var specificHeats = new float[gasTypeCount];
            for (int gasId = 0; gasId < gasTypeCount; gasId++)
                specificHeats[gasId] = 1f;

            return specificHeats;
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
            _pendingReleases.Clear();
            _nextNetworkId = GasPipeNetworkId.NoneValue + 1;
        }
    }
}
