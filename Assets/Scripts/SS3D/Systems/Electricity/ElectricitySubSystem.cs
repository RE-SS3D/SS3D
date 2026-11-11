using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
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
        private static readonly ProfilerMarker FixedUpdatePerformanceMarker = new("SS3D.Electricity.FixedUpdate");
        private static readonly ProfilerMarker CircuitsTickPerformanceMarker = new("SS3D.Electricity.CircuitsTick");
        private static readonly ProfilerMarker AreaPowerPerformanceMarker = new("SS3D.Electricity.AreaPower");
        private static readonly ProfilerMarker OnTickPerformanceMarker = new("SS3D.Electricity.OnTick");

        public event Action WhenReady;

        /// <summary>
        /// Called each time the electricity system updates. Subscribers should use this
        /// instead of Unity update loops.
        /// </summary>
        public event Action OnTick;

        public bool IsReady { get; private set; }

        private record VerticeCoordinates(short X, short Y, byte Layer, byte Direction);

        private bool _graphIsDirty;
        private bool _apcConsumerIndexDirty = true;
        private bool _circuitUpdatesSuspended;
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
        private float _timeElapsed;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _electricityGraph = new();
            _circuits = new();
            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
            SubSystems.Get<TileSubSystem>().RegisterTileMutationObserver(this);
            AwaitAreasThenMarkReady().Forget();
        }

        protected override void OnDestroyed()
        {
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

        [Server]
        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            using (FixedUpdatePerformanceMarker.Auto())
            {
                // Paused while the station is replaced (map load, clear, import), so FixedUpdate never walks
                // devices whose despawn is still draining; the graph is rebuilt once the last scope closes.
                if (WorldSimSuspension.IsSuspended)
                {
                    _circuitUpdatesSuspended = true;
                    return;
                }

                if (_circuitUpdatesSuspended)
                {
                    _circuitUpdatesSuspended = false;
                    _graphIsDirty = true;
                }

                _timeElapsed += Time.deltaTime;

                if (_timeElapsed > _tickRate)
                {
                    HandleCircuitsUpdate();
                    using (OnTickPerformanceMarker.Auto())
                    {
                        RpcInvokeOnTick();
                    }

                    _timeElapsed = 0;
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

                foreach (AreaRecord record in areaSubSystem.GetAllAreas())
                {
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

        [ObserversRpc(RunLocally = true)]
        private void RpcInvokeOnTick()
        {
            OnTick?.Invoke();
        }
    }
}
