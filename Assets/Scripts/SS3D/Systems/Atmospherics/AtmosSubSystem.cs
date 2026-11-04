using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.WorldReadiness;
using SS3D.Logging;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using SS3D.Systems.WorldReadiness;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Server-authoritative atmospherics coordinator. Owns the ECS simulation world and tick loop.
    /// </summary>
    public sealed partial class AtmosSubSystem : NetworkSubSystem, IWorldReady
    {
        private static readonly ProfilerMarker SimPerformanceMarker = new("SS3D.Atmos.Sim");
        private static readonly ProfilerMarker UploadPerformanceMarker = new("SS3D.Atmos.Upload");
        private static readonly ProfilerMarker NetworkSyncPerformanceMarker = new("SS3D.Atmos.NetworkSync");

        [SerializeField] private GasRegistry _gasRegistry;

        private AtmosWorld _atmosWorld;
        private AtmosSimulation _simulation;
        private AtmosTileObserver _tileObserver;
        private AtmosPipeObserver _pipeObserver;
        private AtmosPipeSimulation _pipeSimulation;
        private GasPipeNetworkRegistry _pipeRegistry;
        private readonly AtmosPortRegistry _portRegistry = new();
        private readonly AtmosAreaPortIndex _areaPortIndex = new();
        private AtmosVisualizationBridge _visualizationBridge;
        private AtmosClientVisualizationBridge _clientVisualizationBridge;
        private AtmosDirtyChunkTracker _dirtyChunkTracker;
        private AtmosChunkPatchBuilder _patchBuilder;
        private readonly List<Vector2Int> _dirtyChunkBuffer = new();
        private float _tickTimer;
        private CancellationTokenSource _readinessCts;

        public event Action WhenReady;

        public bool IsReady { get; private set; }

        public GasRegistry GasRegistry => _gasRegistry;
        public float TickInterval => AtmosConstants.TickInterval;
        public AtmosSimulation Simulation => _simulation;
        public AtmosPipeSimulation PipeSimulation => _pipeSimulation;
        public GasPipeNetworkRegistry PipeRegistry => _pipeRegistry;
        public AtmosPortRegistry PortRegistry => _portRegistry;

        /// <summary>
        /// Area → vent/scrubber index. Rebuilds on invalidate (port register or area reflood).
        /// </summary>
        public AtmosAreaPortIndex AreaPortIndex => _areaPortIndex;

        public bool SimulationPaused { get; set; }
        public float LastTickMilliseconds { get; private set; }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!TryGetComponent<AtmosDebugController>(out _))
                gameObject.AddComponent<AtmosDebugController>();

            if (!TryGetComponent<AtmosVisualizationBridge>(out _))
                _visualizationBridge = gameObject.AddComponent<AtmosVisualizationBridge>();
            else
                _visualizationBridge = GetComponent<AtmosVisualizationBridge>();

            _dirtyChunkTracker = new AtmosDirtyChunkTracker();
            _patchBuilder = new AtmosChunkPatchBuilder();

            if (SubSystems.TryGet(out WorldReadinessSubSystem readiness))
            {
                readiness.PhaseChanged += HandleReadinessPhaseChanged;
            }

            InitializeWhenMapReady().Forget();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!TryGetComponent<AtmosWindView>(out _))
                gameObject.AddComponent<AtmosWindView>();

            // Hosts already get visuals through the server-side AtmosVisualizationBridge above;
            // only pure clients need to build their own atlas from networked chunk patches.
            if (IsServer)
                return;

            if (!TryGetComponent<AtmosClientVisualizationBridge>(out _))
                _clientVisualizationBridge = gameObject.AddComponent<AtmosClientVisualizationBridge>();
            else
                _clientVisualizationBridge = GetComponent<AtmosClientVisualizationBridge>();
        }

        private void HandleReadinessPhaseChanged(WorldReadyPhase phase)
        {
            if (phase != WorldReadyPhase.None || !IsServer)
            {
                return;
            }

            DisposeSimulation();
            _dirtyChunkTracker = new AtmosDirtyChunkTracker();
            _dirtyChunkBuffer.Clear();
            InitializeWhenMapReady().Forget();
        }

        private async UniTaskVoid InitializeWhenMapReady()
        {
            _readinessCts?.Cancel();
            _readinessCts?.Dispose();
            _readinessCts = new CancellationTokenSource();
            CancellationToken ct = _readinessCts.Token;

            WorldReadinessSubSystem readiness = null;
            await UniTask.WaitUntil(() => SubSystems.TryGet(out readiness), cancellationToken: ct);

            await readiness.WaitUntilAsync(WorldReadyPhase.TileMapLoaded, ct);

            if (!IsServer || ct.IsCancellationRequested)
                return;

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (tileSubSystem?.CurrentMap == null || tileSubSystem.QueryService == null)
                return;

            if (_simulation != null)
            {
                // Already initialized this epoch — still ensure gate is set after restore.
                readiness.NotifyAtmosReady();
                if (!IsReady)
                {
                    IsReady = true;
                    WhenReady?.Invoke();
                }

                return;
            }

            _atmosWorld = AtmosWorld.Create("AtmosSimulation");

            int gasTypeCount;
            float[] specificHeats = null;
            float[] molarMasses = null;
            if (_gasRegistry != null)
            {
                _gasRegistry.Initialize();
                gasTypeCount = _gasRegistry.GetSlotCount();
                specificHeats = BuildSpecificHeats(_gasRegistry);
                molarMasses = BuildMolarMasses(_gasRegistry);
            }
            else
            {
                Log.Warning(this, "No GasRegistry assigned; using built-in core gas defaults.");
                gasTypeCount = GasDefaults.CoreGasCount;
            }

            _simulation = new AtmosSimulation(
                tileSubSystem.QueryService,
                tileSubSystem.CurrentMap.MapId,
                gasTypeCount,
                specificHeats,
                molarMasses);
            _tileObserver = new AtmosTileObserver(_simulation);
            tileSubSystem.RegisterTileMutationObserver(_tileObserver);

            _pipeRegistry = new GasPipeNetworkRegistry(gasTypeCount);
            _pipeRegistry.RebuildAll(tileSubSystem.CurrentMap);
            _pipeObserver = new AtmosPipeObserver(tileSubSystem.CurrentMap, _pipeRegistry);
            tileSubSystem.RegisterTileMutationObserver(_pipeObserver);
            _pipeSimulation = new AtmosPipeSimulation(
                _pipeRegistry,
                _simulation,
                specificHeats,
                gasTypeCount,
                _pipeObserver);

            // Seed any chunks that were created before the observer registered.
            foreach (TileChunkRef chunkRef in tileSubSystem.CurrentMap.GetChunkRefs())
                _simulation.CreateChunk(chunkRef);

            _visualizationBridge?.PublishSnapshot();

            IsReady = true;
            WhenReady?.Invoke();
            readiness.NotifyAtmosReady();

            Log.Information(this, $"Atmos simulation started with {gasTypeCount} gas slots and {_simulation.CellCount} cells.");
        }

        private static float[] BuildSpecificHeats(GasRegistry registry)
        {
            var heats = new float[AtmosConstants.MaxGasTypes];
            foreach (GasDefinition definition in registry.Definitions)
            {
                if (definition.Id < AtmosConstants.MaxGasTypes)
                    heats[definition.Id] = definition.SpecificHeat;
            }

            return heats;
        }

        private static float[] BuildMolarMasses(GasRegistry registry)
        {
            var masses = new float[AtmosConstants.MaxGasTypes];
            foreach (GasDefinition definition in registry.Definitions)
            {
                if (definition.Id < AtmosConstants.MaxGasTypes)
                    masses[definition.Id] = definition.MolarMass;
            }

            return masses;
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

            DisposeSimulation();
            _visualizationBridge = null;
            _clientVisualizationBridge = null;
            _dirtyChunkTracker = null;
            _patchBuilder = null;
            base.OnDestroyed();
        }

        private void DisposeSimulation()
        {
            IsReady = false;
            SimulationPaused = false;
            _tickTimer = 0f;
            LastTickMilliseconds = 0f;
            SubSystems.TryGet(out TileSubSystem tileSubSystem);
            if (_tileObserver != null)
                tileSubSystem?.UnregisterTileMutationObserver(_tileObserver);
            if (_pipeObserver != null)
                tileSubSystem?.UnregisterTileMutationObserver(_pipeObserver);

            _simulation?.Dispose();
            _simulation = null;
            _tileObserver = null;
            _pipeObserver = null;
            _pipeSimulation = null;
            _pipeRegistry = null;
            _atmosWorld?.Dispose();
            _atmosWorld = null;
            _areaPortIndex.Invalidate();
        }

        private void Update()
        {
            if (!IsServer || _simulation == null || SimulationPaused)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < AtmosConstants.TickInterval)
                return;

            _tickTimer -= AtmosConstants.TickInterval;
            SimTick();
        }

        public void StepOnce()
        {
            if (!IsServer || _simulation == null)
                return;

            SimTick();
        }

        public void DebugWakeRegion(TileCoord coord, int radius)
        {
            if (!IsServer || _simulation == null)
                return;

            _simulation.ActivateRegion(coord, radius);
        }

        public void DebugAddGas(TileCoord coord, GasId gasId, float moles)
        {
            if (!IsServer || _simulation == null)
                return;

            _simulation.DebugAddMoles(coord, gasId, moles);
        }

        public void DebugAddHeat(TileCoord coord, float deltaKelvin)
        {
            if (!IsServer || _simulation == null)
                return;

            _simulation.DebugAddHeat(coord, deltaKelvin);
        }

        private void SimTick()
        {
            float started = Time.realtimeSinceStartup;

            using (SimPerformanceMarker.Auto())
            {
                _simulation.Tick(AtmosConstants.TickInterval);
                _pipeSimulation?.Tick(AtmosConstants.TickInterval);
                _portRegistry.TickDevices(_pipeSimulation, _simulation, AtmosConstants.TickInterval);
                TickWind();
            }

            using (UploadPerformanceMarker.Auto())
            {
                _visualizationBridge?.PublishSnapshot();
            }

            LastTickMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;

            using (NetworkSyncPerformanceMarker.Auto())
            {
                BroadcastDirtyChunks();
            }
        }

        /// <summary>
        /// Sends each visually-dirty chunk to observing clients so pure clients (which have no
        /// local <see cref="AtmosSimulation"/>) can build a matching atlas. See
        /// Documents/architecture/2026-07_atmos-client-visualization-sync.md.
        /// Host GPU visuals use <see cref="AtmosVisualizationBridge.PublishSnapshot"/> — skip
        /// patch build/serialize when there are no remote clients.
        /// </summary>
        private void BroadcastDirtyChunks()
        {
            if (_dirtyChunkTracker == null || _patchBuilder == null)
                return;

            if (!HasRemoteAtmosClients())
                return;

            _dirtyChunkTracker.Update(_simulation);
            _dirtyChunkTracker.ConsumeDirtyChunks(_dirtyChunkBuffer);
            if (_dirtyChunkBuffer.Count == 0)
                return;

            foreach (Vector2Int chunkKey in _dirtyChunkBuffer)
            {
                if (!_simulation.TryGetChunkIndex(chunkKey, out int chunkIndex))
                    continue;

                AtmosChunkPatch patch = _patchBuilder.Build(_simulation, chunkIndex, chunkKey);
                RpcApplyChunkPatch(patch);
            }
        }

        /// <summary>
        /// Pure clients need chunk patches; host-only Play Mode does not (atlas is local).
        /// </summary>
        private bool HasRemoteAtmosClients()
        {
            if (ServerManager == null || ServerManager.Clients.Count == 0)
                return false;

            foreach (NetworkConnection conn in ServerManager.Clients.Values)
            {
                if (conn != null && conn.IsActive && !conn.IsLocalClient)
                    return true;
            }

            return false;
        }

        [ObserversRpc]
        private void RpcApplyChunkPatch(AtmosChunkPatch patch)
        {
            _clientVisualizationBridge?.ApplyChunkPatch(patch);
        }

        public bool TryTransferPipeMoles(
            GasPipeNetworkId networkId,
            GasId gasId,
            float requestedMoles,
            TileCoord turfCell,
            PipeTransferDirection direction,
            out float actuallyMoved)
        {
            actuallyMoved = 0f;
            if (_pipeSimulation == null)
                return false;

            return _pipeSimulation.TryTransferMoles(
                networkId,
                gasId,
                requestedMoles,
                turfCell,
                direction,
                out actuallyMoved);
        }

        public void RegisterPort(IAtmosPortDevice port)
        {
            _portRegistry.Register(port);
            _areaPortIndex.Invalidate();
        }

        public void UnregisterPort(IAtmosPortDevice port)
        {
            _portRegistry.Unregister(port);
            _areaPortIndex.Invalidate();
        }

        /// <summary>
        /// Marks the area→port index dirty after area membership changes (reflood / APC lifecycle).
        /// </summary>
        public void InvalidateAreaPortIndex() => _areaPortIndex.Invalidate();

        /// <summary>
        /// Ensures the area→port index is current, then returns vents/scrubbers for <paramref name="areaId"/>.
        /// </summary>
        public IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> GetIndexedPortsForArea(AreaId areaId)
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return Array.Empty<(AtmosPortControllerBase, AtmosAreaPortKind)>();
            }

            _areaPortIndex.EnsureBuilt(_portRegistry, areaSubSystem);
            return _areaPortIndex.GetPortsInArea(areaId);
        }

        public bool TryGetCellDebugInfo(TileCoord coord, out AtmosCellDebugInfo info)
        {
            if (_simulation == null)
            {
                info = default;
                return false;
            }

            return _simulation.TryGetCellDebugInfo(coord, out info);
        }

        public bool TryGetPipeDebugInfo(GasPipeSegmentKey segment, out AtmosPipeDebugInfo info)
        {
            info = default;
            if (_pipeRegistry == null
                || !_pipeRegistry.TryGetNetworkForSegment(segment, out GasPipeNetworkId networkId, out GasPipeNetworkRecord record))
            {
                return false;
            }

            int gasTypeCount = _simulation?.GasTypeCount ?? AtmosConstants.DefaultGasCount;
            info = new AtmosPipeDebugInfo
            {
                Exists = true,
                Segment = segment,
                NetworkId = networkId,
                SegmentCount = record.Segments.Count,
                PressureKpa = record.GetPressure(gasTypeCount),
                Temperature = record.Temperature,
                Volume = record.Volume,
                TotalMoles = record.GetTotalMoles(gasTypeCount),
            };
            return true;
        }

        public float GetPipeNetworkMoles(GasPipeNetworkId networkId, GasId gasId)
        {
            if (_pipeRegistry == null
                || !_pipeRegistry.TryGetNetwork(networkId, out GasPipeNetworkRecord record)
                || gasId.Value >= record.Moles.Length)
            {
                return 0f;
            }

            return record.Moles[gasId.Value];
        }
    }
}
