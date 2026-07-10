using SS3D.Core;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Electricity;
using System.Text;
using UnityEngine;

namespace System.Electricity
{
    public readonly struct ElectricityDeviceDebugInfo
    {
        public PlacedTileObject TileObject { get; init; }

        public Vector3 WorldPosition { get; init; }

        public int CircuitIndex { get; init; }

        public bool InCircuit { get; init; }

        public string Label { get; init; }
    }

    public partial class ElectricitySubSystem
    {
        public IReadOnlyList<ElectricityDeviceDebugInfo> CollectDeviceDebugInfo()
        {
            var results = new List<ElectricityDeviceDebugInfo>();
            if (_circuits == null)
            {
                return results;
            }

            TileSubSystem tileSubSystem = SubSystems.TryGet(out TileSubSystem subsystem) ? subsystem : null;
            ITileQueryService query = tileSubSystem?.QueryService;

            foreach (IElectricDevice device in _registeredDevices)
            {
                if (device?.TileObject == null || IsCable(device.TileObject))
                {
                    continue;
                }

                int circuitIndex = TryGetCircuitIndex(device, out int index) ? index : -1;
                Vector3 world = query != null
                    ? query.TileToWorld(new TileCoord(device.TileObject.MapId, device.TileObject.WorldOrigin))
                    : device.TileObject.transform.position;

                results.Add(new ElectricityDeviceDebugInfo
                {
                    TileObject = device.TileObject,
                    WorldPosition = world,
                    CircuitIndex = circuitIndex,
                    InCircuit = circuitIndex >= 0,
                    Label = BuildDeviceLabel(device, circuitIndex),
                });
            }

            return results;
        }

        private bool TryGetCircuitIndex(IElectricDevice device, out int index)
        {
            index = -1;
            if (_circuits == null || device == null)
            {
                return false;
            }

            for (int i = 0; i < _circuits.Count; i++)
            {
                if (_circuits[i].ContainsDevice(device))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private string BuildDeviceLabel(IElectricDevice device, int circuitIndex)
        {
            var label = new StringBuilder();
            label.Append(device.TileObject.gameObject.name);
            label.Append('\n');
            label.Append(GetDeviceRole(device));

            if (circuitIndex >= 0)
            {
                label.Append("\nCircuit ");
                label.Append(circuitIndex + 1);
            }
            else
            {
                label.Append("\nNo circuit");
            }

            if (device is IPowerProducer producer)
            {
                label.Append("\nSupply ");
                label.Append(producer.PowerProduction.ToString("0.##"));
                label.Append(" kW");
            }

            if (device is IPowerStorage storage)
            {
                label.Append("\nCell ");
                label.Append(storage.StoredPower.ToString("0.##"));
                label.Append('/');
                label.Append(storage.MaxCapacity.ToString("0.##"));
                label.Append(" kW");
                label.Append(storage.IsOn ? " · output on" : " · output off");
            }

            if (device is IPowerConsumer consumer)
            {
                label.Append("\nLoad ");
                label.Append(consumer.PowerNeeded.ToString("0.##"));
                label.Append(" kW · ");
                label.Append(consumer.Channel);
                label.Append(" · ");
                label.Append(consumer.PowerStatus);
            }

            if (device is IApcChannelSource apc)
            {
                label.Append("\nChannels ");
                label.Append(apc.Channels);
                if (TryGetApcCircuitStats(apc, device as IPowerStorage, out CircuitStats stats))
                {
                    label.Append("\nGrid ");
                    label.Append(stats.TotalSupplyKw.ToString("0.##"));
                    label.Append(" kW · area load ");
                    label.Append(stats.TotalDemandKw.ToString("0.##"));
                    label.Append(" kW");
                }
            }

            if (TryGetCircuitIndex(device, out int index) && index >= 0)
            {
                Circuit circuit = _circuits[index];
                label.Append("\nCircuit surplus ");
                label.Append(circuit.PendingProducerSurplus.ToString("0.##"));
                label.Append(" kW · grid headroom ");
                label.Append(circuit.GetAvailableGridSupplyForArea().ToString("0.##"));
                label.Append(" kW");
            }

            return label.ToString();
        }

        private static string GetDeviceRole(IElectricDevice device)
        {
            if (device is IApcChannelSource)
            {
                return "APC";
            }

            if (device is FuelPowerGenerator)
            {
                return "Fuel generator";
            }

            if (device is IPowerProducer)
            {
                return "Generator";
            }

            if (device is SmesBattery)
            {
                return "SMES";
            }

            if (device is IPowerStorage)
            {
                return "Storage";
            }

            if (device.TileObject != null && device.TileObject.TryGetComponent(out LightPower _))
            {
                return "Light fixture";
            }

            if (device is MachinePowerConsumer)
            {
                return "Machine load";
            }

            if (device is IPowerConsumer)
            {
                return "Consumer";
            }

            return "Electric device";
        }

        private static bool IsCable(PlacedTileObject tileObject)
        {
            return tileObject.Connector is CablesAdjacencyConnector
                || tileObject.GenericType is TileObjectGenericType.Cable or TileObjectGenericType.Wire;
        }
    }
}
