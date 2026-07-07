using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Atmospherics.ECS;
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
        private float _tickTimer;

        public GasRegistry GasRegistry => _gasRegistry;
        public float TickInterval => AtmosConstants.TickInterval;
        public AtmosSimulation Simulation => _simulation;
        public bool SimulationPaused { get; set; }
        public float LastTickMilliseconds { get; private set; }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!TryGetComponent<AtmosDebugController>(out _))
                gameObject.AddComponent<AtmosDebugController>();

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
            if (_gasRegistry != null)
            {
                _gasRegistry.Initialize();
                gasTypeCount = Mathf.Max(_gasRegistry.Count, GasDefaults.CoreGasCount);
            }
            else
            {
                Log.Warning(this, "No GasRegistry assigned; using built-in core gas defaults.");
                gasTypeCount = GasDefaults.CoreGasCount;
            }

            _simulation = new AtmosSimulation(tileSubSystem.QueryService, tileSubSystem.CurrentMap.MapId, gasTypeCount);
            _tileObserver = new AtmosTileObserver(_simulation);
            tileSubSystem.RegisterTileMutationObserver(_tileObserver);

            // Seed any chunks that were created before the observer registered.
            foreach (TileChunkRef chunkRef in tileSubSystem.CurrentMap.GetChunkRefs())
                _simulation.CreateChunk(chunkRef);

            Log.Information(this, $"Atmos simulation started with {gasTypeCount} gas slots and {_simulation.CellCount} cells.");
        }

        protected override void OnDestroyed()
        {
            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (_tileObserver != null)
                tileSubSystem?.UnregisterTileMutationObserver(_tileObserver);

            _simulation?.Dispose();
            _simulation = null;
            _tileObserver = null;
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

        private void SimTick()
        {
            float started = Time.realtimeSinceStartup;
            _simulation.Tick(AtmosConstants.TickInterval);
            LastTickMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
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
    }
}
