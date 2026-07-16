using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Server-side gas pipe network bulk simulation and turf interface transfers.
    /// </summary>
    public sealed class AtmosPipeSimulation
    {
        private readonly GasPipeNetworkRegistry _registry;
        private readonly AtmosSimulation _turf;
        private readonly float[] _specificHeats;
        private readonly int _gasTypeCount;
        private readonly AtmosPipeObserver _observer;

        public GasPipeNetworkRegistry Registry => _registry;

        public AtmosPipeSimulation(
            GasPipeNetworkRegistry registry,
            AtmosSimulation turf,
            float[] specificHeats,
            int gasTypeCount,
            AtmosPipeObserver observer)
        {
            _registry = registry;
            _turf = turf;
            _specificHeats = specificHeats;
            _gasTypeCount = gasTypeCount;
            _observer = observer;
        }

        public void Tick(float deltaTime)
        {
            _observer?.FlushPendingRebuilds();
            UpdateNetworkTemperatures();
        }

        public bool TryTransferMoles(
            GasPipeNetworkId networkId,
            GasId gasId,
            float requestedMoles,
            TileCoord turfCell,
            PipeTransferDirection direction,
            out float actuallyMoved)
        {
            actuallyMoved = 0f;
            if (requestedMoles <= 0f
                || networkId.IsNone
                || gasId.Value >= _gasTypeCount
                || !_registry.TryGetNetwork(networkId, out GasPipeNetworkRecord network))
            {
                return false;
            }

            if (!_turf.TryGetCellIndex(turfCell, out int cellIndex))
                return false;

            switch (direction)
            {
                case PipeTransferDirection.ToNetwork:
                    return TransferTurfToNetwork(network, gasId, requestedMoles, turfCell, cellIndex, out actuallyMoved);
                case PipeTransferDirection.ToTurf:
                    return TransferNetworkToTurf(network, gasId, requestedMoles, turfCell, cellIndex, out actuallyMoved);
                default:
                    return false;
            }
        }

        public float GetTotalMolesAcrossNetworks()
        {
            float total = 0f;
            foreach (GasPipeNetworkRecord record in _registry.Networks.Values)
                total += record.GetTotalMoles(_gasTypeCount);

            return total;
        }

        private bool TransferTurfToNetwork(
            GasPipeNetworkRecord network,
            GasId gasId,
            float requestedMoles,
            TileCoord turfCell,
            int cellIndex,
            out float actuallyMoved)
        {
            actuallyMoved = 0f;
            if (!_turf.TryRemoveMoles(turfCell, gasId, requestedMoles, out float removed, out float sourceTemperature))
                return false;

            actuallyMoved = removed;
            if (actuallyMoved <= 0f)
                return false;

            float specificHeat = GetSpecificHeat(gasId);
            float currentCapacity = network.GetHeatCapacity(_specificHeats, _gasTypeCount);
            network.Moles[gasId.Value] += actuallyMoved;
            network.Temperature = AtmosPipeThermo.BlendTemperature(
                network.Temperature,
                currentCapacity,
                actuallyMoved,
                specificHeat,
                sourceTemperature);

            _turf.ActivateRegion(turfCell, 1);
            return true;
        }

        private bool TransferNetworkToTurf(
            GasPipeNetworkRecord network,
            GasId gasId,
            float requestedMoles,
            TileCoord turfCell,
            int cellIndex,
            out float actuallyMoved)
        {
            actuallyMoved = 0f;
            float available = network.Moles[gasId.Value];
            if (available <= 0f)
                return false;

            actuallyMoved = Mathf.Min(requestedMoles, available);
            if (actuallyMoved <= 0f)
                return false;

            float specificHeat = GetSpecificHeat(gasId);
            float sourceTemperature = network.Temperature;
            network.Moles[gasId.Value] -= actuallyMoved;

            if (network.GetTotalMoles(_gasTypeCount) <= 0f)
                network.Temperature = AtmosConstants.StandardTemperature;

            if (!_turf.TryAddMolesAtTemperature(turfCell, gasId, actuallyMoved, sourceTemperature))
            {
                network.Moles[gasId.Value] += actuallyMoved;
                return false;
            }

            _turf.ActivateRegion(turfCell, 1);
            return true;
        }

        private void UpdateNetworkTemperatures()
        {
            foreach (GasPipeNetworkRecord record in _registry.Networks.Values)
            {
                if (record.Segments.Count == 0)
                    continue;

                float sumTemperature = 0f;
                int sampleCount = 0;

                foreach (GasPipeSegmentKey segment in record.Segments)
                {
                    if (_turf.TryGetCellTemperature(segment.Coord, out float temperature))
                    {
                        sumTemperature += temperature;
                        sampleCount++;
                    }
                }

                if (sampleCount > 0)
                    record.Temperature = sumTemperature / sampleCount;
            }
        }

        private float GetSpecificHeat(GasId gasId)
        {
            if (_specificHeats == null || gasId.Value >= _specificHeats.Length)
                return 1f;

            float value = _specificHeats[gasId.Value];
            return value > 0f ? value : 1f;
        }
    }
}
