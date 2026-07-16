using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Server-authoritative atmospherics coordinator. Owns the ECS simulation world and tick loop.
    /// </summary>
    public sealed class AtmosSubSystem : NetworkSubSystem
    {
        [SerializeField] private GasRegistry _gasRegistry;

        private AtmosWorld _atmosWorld;
        private AtmosSimulation _simulation;
        private AtmosTileObserver _tileObserver;
        private AtmosPipeObserver _pipeObserver;
        private AtmosPipeSimulation _pipeSimulation;
        private GasPipeNetworkRegistry _pipeRegistry;
        private readonly AtmosPortRegistry _portRegistry = new();
        private AtmosVisualizationBridge _visualizationBridge;
        private float _tickTimer;

        public GasRegistry GasRegistry => _gasRegistry;
        public float TickInterval => AtmosConstants.TickInterval;
        public AtmosSimulation Simulation => _simulation;
        public AtmosPipeSimulation PipeSimulation => _pipeSimulation;
        public GasPipeNetworkRegistry PipeRegistry => _pipeRegistry;
        public AtmosPortRegistry PortRegistry => _portRegistry;
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

            InitializeWhenMapReady().Forget();
        }

        private async UniTaskVoid InitializeWhenMapReady()
        {
            TileSubSystem tileSubSystem = null;
            await UniTask.WaitUntil(() =>
            {
                tileSubSystem = SubSystems.Get<TileSubSystem>();
                return tileSubSystem != null
                    && tileSubSystem.CurrentMap != null
                    && tileSubSystem.QueryService != null;
            });

            if (!IsServer)
                return;

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
            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
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
            _visualizationBridge = null;
            _atmosWorld?.Dispose();
            _atmosWorld = null;
            base.OnDestroyed();
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
            _simulation.Tick(AtmosConstants.TickInterval);
            _pipeSimulation?.Tick(AtmosConstants.TickInterval);
            _portRegistry.TickDevices(_pipeSimulation, _simulation, AtmosConstants.TickInterval);
            LastTickMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
            _visualizationBridge?.PublishSnapshot();
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

        public void RegisterPort(IAtmosPortDevice port) => _portRegistry.Register(port);

        public void UnregisterPort(IAtmosPortDevice port) => _portRegistry.Unregister(port);

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
