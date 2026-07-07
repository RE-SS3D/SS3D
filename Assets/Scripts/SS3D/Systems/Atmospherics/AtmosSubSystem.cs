using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Atmospherics.ECS;
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
        private float _tickTimer;

        public GasRegistry GasRegistry => _gasRegistry;
        public float TickInterval => AtmosConstants.TickInterval;

        [ServerOrClient]
        protected override void OnStart()
        {
            base.OnStart();

            if (!IsServer)
                return;

            if (_gasRegistry == null)
            {
                Log.Error(this, "GasRegistry is not assigned on AtmosSubSystem.");
                return;
            }

            _gasRegistry.Initialize();
            _atmosWorld = AtmosWorld.Create("AtmosSimulation");
            Log.Information(this, $"Atmos ECS world created with {_gasRegistry.Count} gases.");
        }

        private void Update()
        {
            if (!IsServer || _atmosWorld == null)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < AtmosConstants.TickInterval)
                return;

            _tickTimer -= AtmosConstants.TickInterval;
            SimTick();
        }

        private void SimTick()
        {
            // Phase 1: observer flush, active list, ShareGasJob.
        }

        private void OnDestroy()
        {
            _atmosWorld?.Dispose();
            _atmosWorld = null;
        }
    }
}
