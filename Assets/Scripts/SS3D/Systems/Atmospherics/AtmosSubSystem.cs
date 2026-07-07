using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Bridge;
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

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_gasRegistry == null)
            {
                Log.Error(this, "GasRegistry is not assigned on AtmosSubSystem.");
                return;
            }

            _gasRegistry.Initialize();
            _atmosWorld = AtmosWorld.Create("AtmosSimulation");

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (tileSubSystem?.QueryService == null || tileSubSystem.CurrentMap == null)
            {
                Log.Error(this, "TileSubSystem map is not ready for atmospherics.");
                return;
            }

            int gasTypeCount = Mathf.Max(_gasRegistry.Count, AtmosConstants.DefaultGasCount);
            _simulation = new AtmosSimulation(tileSubSystem.QueryService, tileSubSystem.CurrentMap.MapId, gasTypeCount);
            _tileObserver = new AtmosTileObserver(_simulation);
            tileSubSystem.RegisterTileMutationObserver(_tileObserver);

            Log.Information(this, $"Atmos simulation started with {gasTypeCount} gas slots.");
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
            if (!IsServer || _simulation == null)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < AtmosConstants.TickInterval)
                return;

            _tickTimer -= AtmosConstants.TickInterval;
            SimTick();
        }

        private void SimTick()
        {
            _simulation.Tick(AtmosConstants.TickInterval);
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
