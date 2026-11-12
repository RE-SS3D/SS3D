using Cysharp.Threading.Tasks;
using FishNet.Object;
using QuikGraph;
using QuikGraph.Algorithms;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.WorldReadiness;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.WorldReadiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Profiling;
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
    public partial class ElectricitySubSystem : NetworkSubSystem, ITileMutationObserver, IWorldReady
    {
        private static readonly ProfilerMarker TickPerformanceMarker = new("SS3D.Electricity.Tick");
        private static readonly ProfilerMarker CircuitsTickPerformanceMarker = new("SS3D.Electricity.CircuitsTick");
        private static readonly ProfilerMarker AreaPowerPerformanceMarker = new("SS3D.Electricity.AreaPower");
        private static readonly ProfilerMarker OnTickPerformanceMarker = new("SS3D.Electricity.OnTick");

        public event Action WhenReady;

        /// <summary>
        /// Called each time the electricity system updates, on the server and on every client (each runs the
        /// electricity channel of <see cref="SimulationClock"/>). Subscribers should use this instead of Unity update loops.
        /// </summary>
        public event Action OnTick;

        public bool IsReady { get; private set; }

        private record VerticeCoordinates(short X, short Y, byte Layer, byte Direction);

        private bool _graphIsDirty;
        private bool _apcConsumerIndexDirty = true;
        private List<Circuit> _circuits;
        private readonly Dictionary<IElectricDevice, Circuit> _circuitByDevice = new();
        private readonly List<IPowerConsumer> _registeredConsumers = new();
        private readonly List<IElectricDevice> _registeredDevices = new();
        private readonly Dictionary<IApcChannelSource, List<IPowerConsumer>> _consumersByApc = new();
        private readonly Dictionary<IPowerConsumer, IApcChannelSource> _apcByConsumer = new();
        private readonly HashSet<IPowerConsumer> _areaScopedConsumers = new();
        private readonly Dictionary<IApcChannelSource, float> _lastApcGridInputKw = new();
        private readonly Dictionary<IApcChannelSource, float> _lastApcGridAvailableKw = new();
        private readonly List<IPowerConsumer> _areaActiveConsumersScratch = new();
        private readonly List<IPowerConsumer> _areaPoweredConsumersScratch = new();
        private readonly HashSet<IPowerConsumer> _areaPoweredSetScratch = new();
        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;
        private CancellationTokenSource _readinessCts;

        [SerializeField]
        private float _tickRate = 0.2f;
        private SimulationChannel _channel;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // World suspension (map load, clear, import) pauses this channel, so ticks never walk devices whose
            // despawn is still draining; the graph is rebuilt on the first tick after the last scope closes.
            _channel = SimulationClock.GetOrCreate("electricity", _tickRate, pausedByWorldSuspension: true);
            _channel.Resumed -= HandleChannelResumed;
            _channel.Resumed += HandleChannelResumed;
            _channel.Subscribe(HandleChannelTick);
        }

        /// <summary>
        /// A client's channel stops with the session. The ObserversRpc this replaced stopped on disconnect; a channel that
        /// kept firing would tick devices whose objects are being destroyed (mppm:reconnect).
        /// </summary>
        public override void OnStopNetwork()
        {
            UnsubscribeFromChannel();
            base.OnStopNetwork();
        }

        private void UnsubscribeFromChannel()
        {
            if (_channel == null) return;
            _channel.Unsubscribe(HandleChannelTick);
            _channel.Resumed -= HandleChannelResumed;
            _channel = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _electricityGraph = new();
            _circuits = new();
            SubSystems.Get<TileSubSystem>().RegisterTileMutationObserver(this);
            AwaitAreasThenMarkReady().Forget();
        }

        protected override void OnDestroyed()
        {
            UnsubscribeFromChannel();

            _readinessCts?.Cancel();
            _readinessCts?.Dispose();
            _readinessCts = null;

            if (SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                readiness.PhaseChanged -= HandleReadinessPhaseChanged;
            }

            SubSystems.GetOrNull<TileSubSystem>()?.UnregisterTileMutationObserver(this);
            base.OnDestroyed();
        }

        private async UniTaskVoid AwaitAreasThenMarkReady()
        {
            _readinessCts?.Cancel();
            _readinessCts?.Dispose();
            _readinessCts = new CancellationTokenSource();
            CancellationToken ct = _readinessCts.Token;

            if (!SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                MarkReady();
                return;
            }

            readiness.PhaseChanged -= HandleReadinessPhaseChanged;
            readiness.PhaseChanged += HandleReadinessPhaseChanged;

            try
            {
                await readiness.WaitUntilAsync(WorldReadyPhase.AreasFlooded, ct);
                if (!ct.IsCancellationRequested)
                {
                    MarkReady();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HandleReadinessPhaseChanged(WorldReadyPhase phase)
        {
            if (phase != WorldReadyPhase.None || !IsServer)
            {
                return;
            }

            // Station restore reset — clear ready and wait for AreasFlooded again.
            IsReady = false;
            AwaitAreasThenMarkReady().Forget();
        }

        private void MarkReady()
        {
            if (IsReady)
            {
                return;
            }

            IsReady = true;
            WhenReady?.Invoke();

            if (SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                readiness.NotifyElectricityReady();
            }
        }

        [Server]
        public bool TryGetCircuitStats(IElectricDevice device, IPowerStorage apcCell, out CircuitStats stats)
        {
            stats = default;
            Circuit circuit = TryGetCircuitForDevice(device);
            if (circuit == null)
            {
                return false;
            }

            stats = circuit.GetStats(apcCell);
            return true;
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
            AreaApcPowerDistribution.FillActiveConsumers(areaConsumers, apc.Channels, _areaActiveConsumersScratch);
            float gridInputKw = GetApcGridInputKw(apc);
            stats = AreaApcPowerDistribution.BuildApcStats(gridInputKw, apcCell, _areaActiveConsumersScratch);
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
        /// Drop all registered devices/circuits. Call after <see cref="TileMap.Clear"/> when despawn
        /// may lag behind the emptied chunk dictionary.
        /// </summary>
        public void ClearRegisteredDevices()
        {
            _registeredDevices.Clear();
            _registeredConsumers.Clear();
            _lastApcGridInputKw.Clear();
            _lastApcGridAvailableKw.Clear();
            _consumersByApc.Clear();
            _apcByConsumer.Clear();
            _areaScopedConsumers.Clear();
            _circuits.Clear();
            _circuitByDevice.Clear();
            _electricityGraph?.Clear();
            _graphIsDirty = true;
            _apcConsumerIndexDirty = true;
        }

        /// <summary>
        /// Marks the APC→consumer index dirty after area membership changes.
        /// Call from Area APC register/unregister/rebuild paths.
        /// </summary>
        public void InvalidateAreaConsumerIndex()
        {
            _apcConsumerIndexDirty = true;
        }

        private void HandleChannelResumed() => _graphIsDirty = true;

        private void HandleChannelTick()
        {
            // Between the transport dropping and OnStopNetwork, a client's objects are already going away.
            if (!IsServer && (ClientManager == null || !ClientManager.Started)) return;
            using (TickPerformanceMarker.Auto())
            {
                if (IsServer && _circuits != null)
                {
                    HandleCircuitsUpdate();
                }

                using (OnTickPerformanceMarker.Auto())
                {
                    OnTick?.Invoke();
                }
            }
        }

        [Server]
        public void HandleCircuitsUpdate()
        {
            using (CircuitsTickPerformanceMarker.Auto())
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
        }

        [Server]
        private void UpdateAreaScopedPower()
        {
            using (AreaPowerPerformanceMarker.Auto())
            {
                if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
                {
                    return;
                }

                EnsureApcConsumerIndex();

                // Indexed: foreach over the IReadOnlyList boxes its enumerator every tick.
                IReadOnlyList<AreaRecord> areas = areaSubSystem.GetAllAreas();
                for (int i = 0; i < areas.Count; i++)
                {
                    AreaRecord record = areas[i];
                    if (record.Apc is not IApcChannelSource apc
                        || record.Apc is not IPowerStorage apcStorage
                        || record.Apc is not IElectricDevice apcDevice
                        || !TryGetLiveTileObject(apcDevice, out _))
                    {
                        continue;
                    }

                    IReadOnlyList<IPowerConsumer> areaConsumers = GetIndexedConsumersForApc(apc);
                    AreaApcPowerDistribution.FillActiveConsumers(areaConsumers, apc.Channels, _areaActiveConsumersScratch);
                    float demandKw = AreaApcPowerDistribution.SumPowerNeeded(_areaActiveConsumersScratch);
                    float gridAvailableKw = GetAvailableGridSupplyForApc(apcDevice);
                    float gridDrawKw = Math.Min(demandKw, gridAvailableKw);
                    _lastApcGridAvailableKw[apc] = gridAvailableKw;
                    _lastApcGridInputKw[apc] = gridDrawKw;
                    TryGetCircuitForDevice(apcDevice)?.DrawGridPowerForArea(gridDrawKw, _tickRate);
                    AreaApcPowerDistribution.PowerAreaConsumers(
                        apc,
                        apcStorage,
                        gridDrawKw,
                        areaConsumers,
                        _areaActiveConsumersScratch,
                        _tickRate,
                        _areaPoweredConsumersScratch,
                        _areaPoweredSetScratch);
                }
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

            _apcByConsumer.Clear();
            _areaScopedConsumers.Clear();

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
                _apcByConsumer[consumer] = apc;
                _areaScopedConsumers.Add(consumer);
            }
        }

        /// <summary>
        /// True when the consumer draws from an area APC (not cable-only). Uses the rebuilt index.
        /// </summary>
        internal bool IsIndexedAreaScopedConsumer(IPowerConsumer consumer)
        {
            EnsureApcConsumerIndex();
            return consumer != null && _areaScopedConsumers.Contains(consumer);
        }

        internal bool TryGetIndexedApcForConsumer(IPowerConsumer consumer, out IApcChannelSource apc)
        {
            EnsureApcConsumerIndex();
            if (consumer != null && _apcByConsumer.TryGetValue(consumer, out apc))
            {
                return true;
            }

            apc = null;
            return false;
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
            if (device == null || !_circuitByDevice.TryGetValue(device, out Circuit circuit))
            {
                return null;
            }

            return circuit;
        }

        [Server]
        public void AddElectricalElement(IElectricDevice device)
        {
            if (_electricityGraph == null || !TryGetLiveTileObject(device, out _))
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
            // Must unregister even when TileObject is already null — BasicElectricDevice.OnDestroyed
            // runs after Unity considers the GO destroyed, so TileObject returns null. Requiring a
            // live tile left zombies in _registeredDevices → NRE on RebuildElectricGraph (map Clear /
            // DMM import). Hit 2026-07-28.
            if (_electricityGraph == null || device == null)
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
            TileMap map = SubSystems.TryGet(out TileSubSystem tileSystem) ? tileSystem.CurrentMap : null;

            for (int i = _registeredDevices.Count - 1; i >= 0; i--)
            {
                IElectricDevice device = _registeredDevices[i];
                if (!TryGetLiveTileObject(device, out PlacedTileObject tileObject)
                    || IsOrphanedAfterMapClear(map, tileObject))
                {
                    _registeredDevices.RemoveAt(i);
                    if (device is IPowerConsumer consumer)
                        _registeredConsumers.Remove(consumer);
                    continue;
                }

                AddDeviceEdgesToGraph(device, tileObject);
            }
        }

        /// <summary>
        /// Interface-typed <see cref="IElectricDevice"/> skips Unity fake-null on <c>?.</c>.
        /// Accessing <see cref="IElectricDevice.TileObject"/> on a destroyed <see cref="ApcController"/>
        /// throws MissingReferenceException — check UnityEngine.Object first.
        /// </summary>
        private static bool TryGetLiveTileObject(IElectricDevice device, out PlacedTileObject tileObject)
        {
            tileObject = null;
            if (device == null)
                return false;

            if (device is UnityEngine.Object unityObject && !unityObject)
                return false;

            tileObject = device.TileObject;
            return tileObject != null;
        }

        /// <summary>
        /// TileMap.Clear empties the chunk dictionary before FishNet finishes despawning devices.
        /// Those zombies still report a non-null TileObject but have no chunk — prune them.
        /// </summary>
        private static bool IsOrphanedAfterMapClear(TileMap map, PlacedTileObject tileObject)
        {
            if (map == null || tileObject == null)
                return true;

            Vector3 world = new(tileObject.WorldOrigin.x, 0f, tileObject.WorldOrigin.y);
            return map.GetChunk(world) == null;
        }

        [Server]
        private void AddDeviceEdgesToGraph(IElectricDevice device, PlacedTileObject tileObject)
        {
            if (tileObject == null)
                return;

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
                if (neighbour == null)
                    continue;

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
            _circuitByDevice.Clear();

            Dictionary<int, List<VerticeCoordinates>> graphs = components.GroupBy(pair => pair.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Key).ToList());

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem?.CurrentMap;

            foreach (List<VerticeCoordinates> component in graphs.Values)
            {
                Circuit circuit = new Circuit();
                foreach (VerticeCoordinates coord in component)
                {
                    if (map == null)
                        continue;

                    ITileLocation location = map.GetTileLocation(
                        (TileLayer)coord.Layer,
                        new(coord.X, 0f, coord.Y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.Direction))
                        continue;

                    if (!placedObject.TryGetComponent(out IElectricDevice device))
                        continue;

                    circuit.AddElectricDevice(device);
                    _circuitByDevice[device] = circuit;
                }

                circuit.SetConsumerChannelResolver(consumer => ResolveEnabledChannelsForConsumer(circuit, consumer));
                circuit.SetCableDistributionFilter(consumer => !IsIndexedAreaScopedConsumer(consumer));
                _circuits.Add(circuit);
            }
        }

        private ApcControlFlags ResolveEnabledChannelsForConsumer(Circuit circuit, IPowerConsumer consumer)
        {
            if (TryGetIndexedApcForConsumer(consumer, out IApcChannelSource indexedApc))
            {
                return indexedApc.Channels;
            }

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
    }
}
