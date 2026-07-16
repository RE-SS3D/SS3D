using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using QuikGraph;
using QuikGraph.Algorithms;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Handles a graph that contains all electricity circuits.
    /// </summary>
    /// <remarks>
    /// Graph topology is marked dirty on changes and rebuilt on the next tick.
    /// Cable placement uses <see cref="ITileMutationObserver"/> to refresh device edges.
    /// </remarks>
    public partial class ElectricitySubSystem : NetworkSubSystem, ITileMutationObserver
    {
        public event Action OnSystemSetUp;

        /// <summary>
        /// Called each time the electricity system updates. Subscribers should use this
        /// instead of Unity update loops.
        /// </summary>
        public event Action OnTick;

        public bool IsSetUp { get; private set; }

        private record VerticeCoordinates(short X, short Y, byte Layer, byte Direction);

        private bool _graphIsDirty;
        private bool _apcConsumerIndexDirty = true;
        private List<Circuit> _circuits;
        private readonly List<IPowerConsumer> _registeredConsumers = new();
        private readonly List<IElectricDevice> _registeredDevices = new();
        private readonly Dictionary<IApcChannelSource, List<IPowerConsumer>> _consumersByApc = new();
        private readonly Dictionary<IApcChannelSource, float> _lastApcGridInputKw = new();
        private readonly Dictionary<IApcChannelSource, float> _lastApcGridAvailableKw = new();
        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;

        [SerializeField]
        private float _tickRate = 0.2f;
        private float _timeElapsed;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _electricityGraph = new();
            _circuits = new();
            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
            SubSystems.Get<TileSubSystem>().RegisterTileMutationObserver(this);
            IsSetUp = true;
            OnSystemSetUp?.Invoke();
        }

        protected override void OnDestroyed()
        {
            SubSystems.Get<TileSubSystem>()?.UnregisterTileMutationObserver(this);
            base.OnDestroyed();
        }

        [Server]
        public bool TryGetCircuitStats(IElectricDevice device, IPowerStorage apcCell, out CircuitStats stats)
        {
            stats = default;
            if (_circuits == null)
            {
                return false;
            }

            foreach (Circuit circuit in _circuits)
            {
                if (!circuit.ContainsDevice(device))
                {
                    continue;
                }

                stats = circuit.GetStats(apcCell);
                return true;
            }

            return false;
        }

        [Server]
        public bool TryGetApcCircuitStats(IApcChannelSource apc, IPowerStorage apcCell, out CircuitStats stats)
        {
            stats = default;
            if (apc == null)
            {
                return false;
            }

            EnsureApcConsumerIndex();
            IReadOnlyList<IPowerConsumer> areaConsumers = GetIndexedConsumersForApc(apc);
            List<IPowerConsumer> activeConsumers = AreaApcPowerDistribution.GetActiveConsumers(areaConsumers, apc.Channels);
            float gridInputKw = GetApcGridInputKw(apc);
            stats = AreaApcPowerDistribution.BuildApcStats(gridInputKw, apcCell, activeConsumers);
            if (_lastApcGridAvailableKw.TryGetValue(apc, out float gridAvailableKw))
            {
                stats.GridAvailableKw = gridAvailableKw;
            }
            else if (apc is IElectricDevice apcDevice)
            {
                stats.GridAvailableKw = GetAvailableGridSupplyForApc(apcDevice);
            }

            return true;
        }

        /// <summary>
        /// Marks the APC→consumer index dirty after area membership changes.
        /// Call from Area APC register/unregister/rebuild paths.
        /// </summary>
        public void InvalidateAreaConsumerIndex()
        {
            _apcConsumerIndexDirty = true;
        }

        [Server]
        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            _timeElapsed += Time.deltaTime;

            if (_timeElapsed > _tickRate)
            {
                HandleCircuitsUpdate();
                RpcInvokeOnTick();
                _timeElapsed = 0;
            }
        }

        [Server]
        public void HandleCircuitsUpdate()
        {
            if (_graphIsDirty)
            {
                RebuildElectricGraph();
                UpdateAllCircuitsTopology();
                _graphIsDirty = false;
            }

            foreach (Circuit circuit in _circuits)
            {
                circuit.UpdateCableDistributionOnly(_tickRate);
            }

            UpdateAreaScopedPower();

            foreach (Circuit circuit in _circuits)
            {
                circuit.ChargePendingProducerSurplus(_tickRate);
            }
        }

        [Server]
        private void UpdateAreaScopedPower()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            EnsureApcConsumerIndex();

            foreach (AreaRecord record in areaSubSystem.GetAllAreas())
            {
                if (record.Apc is not IApcChannelSource apc
                    || record.Apc is not IPowerStorage apcStorage
                    || record.Apc is not IElectricDevice apcDevice)
                {
                    continue;
                }

                IReadOnlyList<IPowerConsumer> areaConsumers = GetIndexedConsumersForApc(apc);
                List<IPowerConsumer> activeConsumers = AreaApcPowerDistribution.GetActiveConsumers(areaConsumers, apc.Channels);
                float demandKw = AreaApcPowerDistribution.SumPowerNeeded(activeConsumers);
                float gridAvailableKw = GetAvailableGridSupplyForApc(apcDevice);
                float gridDrawKw = Math.Min(demandKw, gridAvailableKw);
                _lastApcGridAvailableKw[apc] = gridAvailableKw;
                _lastApcGridInputKw[apc] = gridDrawKw;
                TryGetCircuitForDevice(apcDevice)?.DrawGridPowerForArea(gridDrawKw, _tickRate);
                AreaApcPowerDistribution.PowerAreaConsumers(apc, apcStorage, gridDrawKw, areaConsumers, activeConsumers, _tickRate);
            }
        }

        [Server]
        private void EnsureApcConsumerIndex()
        {
            if (!_apcConsumerIndexDirty)
            {
                return;
            }

            RebuildApcConsumerIndex();
            _apcConsumerIndexDirty = false;
        }

        [Server]
        private void RebuildApcConsumerIndex()
        {
            foreach (KeyValuePair<IApcChannelSource, List<IPowerConsumer>> entry in _consumersByApc)
            {
                entry.Value.Clear();
            }

            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            foreach (IPowerConsumer consumer in _registeredConsumers)
            {
                if (consumer is not IElectricDevice device
                    || !areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource apc))
                {
                    continue;
                }

                if (!_consumersByApc.TryGetValue(apc, out List<IPowerConsumer> list))
                {
                    list = new List<IPowerConsumer>();
                    _consumersByApc[apc] = list;
                }

                list.Add(consumer);
            }
        }

        private IReadOnlyList<IPowerConsumer> GetIndexedConsumersForApc(IApcChannelSource apc)
        {
            if (_consumersByApc.TryGetValue(apc, out List<IPowerConsumer> list))
            {
                return list;
            }

            return Array.Empty<IPowerConsumer>();
        }

        [Server]
        private float GetApcGridInputKw(IApcChannelSource apc)
        {
            if (_lastApcGridInputKw.TryGetValue(apc, out float lastGridInputKw))
            {
                return lastGridInputKw;
            }

            return apc is IElectricDevice apcDevice ? GetAvailableGridSupplyForApc(apcDevice) : 0f;
        }

        [Server]
        private float GetAvailableGridSupplyForApc(IElectricDevice apcDevice)
        {
            return TryGetCircuitForDevice(apcDevice)?.GetAvailableGridSupplyForArea(_tickRate) ?? 0f;
        }

        [Server]
        private Circuit TryGetCircuitForDevice(IElectricDevice device)
        {
            if (_circuits == null || device == null)
            {
                return null;
            }

            foreach (Circuit circuit in _circuits)
            {
                if (circuit.ContainsDevice(device))
                {
                    return circuit;
                }
            }

            return null;
        }

        [Server]
        public void AddElectricalElement(IElectricDevice device)
        {
            if (_electricityGraph == null || device?.TileObject == null)
            {
                return;
            }

            if (!_registeredDevices.Contains(device))
            {
                _registeredDevices.Add(device);
            }

            if (device is IPowerConsumer consumer && !_registeredConsumers.Contains(consumer))
            {
                _registeredConsumers.Add(consumer);
            }

            _graphIsDirty = true;
            _apcConsumerIndexDirty = true;
        }

        [Server]
        public void RemoveElectricalElement(IElectricDevice device)
        {
            if (_electricityGraph == null || device?.TileObject == null)
            {
                return;
            }

            _registeredDevices.Remove(device);

            if (device is IPowerConsumer consumer)
            {
                _registeredConsumers.Remove(consumer);
            }

            if (device is IApcChannelSource apc)
            {
                _lastApcGridInputKw.Remove(apc);
                _lastApcGridAvailableKw.Remove(apc);
                _consumersByApc.Remove(apc);
            }

            _graphIsDirty = true;
            _apcConsumerIndexDirty = true;
        }

        [Server]
        private void RebuildElectricGraph()
        {
            _electricityGraph.Clear();
            foreach (IElectricDevice device in _registeredDevices)
            {
                AddDeviceEdgesToGraph(device);
            }
        }

        [Server]
        private void AddDeviceEdgesToGraph(IElectricDevice device)
        {
            PlacedTileObject tileObject = device.TileObject;
            VerticeCoordinates deviceCoordinates = ToCoordinates(tileObject);

            if (!_electricityGraph.ContainsVertex(deviceCoordinates))
            {
                _electricityGraph.AddVertex(deviceCoordinates);
            }

            List<PlacedTileObject> neighbours = tileObject.Connector?.GetNeighbours();
            if (neighbours == null)
            {
                return;
            }

            foreach (PlacedTileObject neighbour in neighbours)
            {
                VerticeCoordinates neighbourCoordinates = ToCoordinates(neighbour);
                if (!_electricityGraph.ContainsVertex(neighbourCoordinates))
                {
                    _electricityGraph.AddVertex(neighbourCoordinates);
                }

                if (!_electricityGraph.TryGetEdge(deviceCoordinates, neighbourCoordinates, out _))
                {
                    _electricityGraph.AddEdge(new(deviceCoordinates, neighbourCoordinates));
                }
            }
        }

        [Server]
        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            if (occupant is not PlacedTileObject placed)
                return;

            if (placed.Connector is CablesAdjacencyConnector)
                RefreshNeighbouringElectricDevices(placed);
        }

        [Server]
        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            if (occupant is not PlacedTileObject placed)
                return;

            if (placed.Connector is CablesAdjacencyConnector)
                RefreshNeighbouringElectricDevices(placed);
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
        }

        public void OnTileStateChanged(TileCoord coord)
        {
        }

        [Server]
        private void RefreshNeighbouringElectricDevices(PlacedTileObject topologyTile)
        {
            foreach (PlacedTileObject neighbour in ElectricNeighbourLookup.GetNeighbours(topologyTile))
            {
                if (neighbour.TryGetComponent(out IElectricDevice device))
                    RefreshElectricalElement(device);
            }
        }

        [Server]
        private void RefreshElectricalElement(IElectricDevice device)
        {
            RemoveElectricalElement(device);
            AddElectricalElement(device);
        }

        [Server]
        private void UpdateAllCircuitsTopology()
        {
            Dictionary<VerticeCoordinates, int> components = new();
            _electricityGraph.ConnectedComponents(components);
            _circuits.Clear();

            Dictionary<int, List<VerticeCoordinates>> graphs = components.GroupBy(pair => pair.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Key).ToList());

            foreach (List<VerticeCoordinates> component in graphs.Values)
            {
                Circuit circuit = new Circuit();
                foreach (VerticeCoordinates coord in component)
                {
                    TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
                    ITileLocation location = tileSystem.CurrentMap.GetTileLocation(
                        (TileLayer)coord.Layer,
                        new(coord.X, 0f, coord.Y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.Direction))
                        continue;

                    if (!placedObject.TryGetComponent(out IElectricDevice device))
                        continue;

                    circuit.AddElectricDevice(device);
                }

                circuit.SetConsumerChannelResolver(consumer => ResolveEnabledChannelsForConsumer(circuit, consumer));
                circuit.SetCableDistributionFilter(consumer => !AreaApcPowerDistribution.IsAreaScopedConsumer(consumer));
                _circuits.Add(circuit);
            }
        }

        private static ApcControlFlags ResolveEnabledChannelsForConsumer(Circuit circuit, IPowerConsumer consumer)
        {
            if (consumer is IElectricDevice device
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource areaApc))
            {
                return areaApc.Channels;
            }

            return circuit.GetCircuitWideEnabledChannels();
        }

        private static VerticeCoordinates ToCoordinates(PlacedTileObject tileObject) =>
            new((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y,
                (byte)tileObject.Layer, (byte)tileObject.Direction);

        [ObserversRpc(RunLocally = true)]
        private void RpcInvokeOnTick()
        {
            OnTick?.Invoke();
        }
    }
}
