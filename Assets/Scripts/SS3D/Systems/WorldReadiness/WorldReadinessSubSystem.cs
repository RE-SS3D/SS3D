using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.WorldReadiness;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Persistence;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SS3D.Systems.WorldReadiness
{
    /// <summary>
    /// Coordinates world-simulation readiness phases. Server notifies; round start and domain
    /// init await phases instead of fixed delays or <c>CurrentMap != null</c> polls.
    /// </summary>
    public sealed class WorldReadinessSubSystem : SubSystem
    {
        private byte _gates;
        private bool _persistenceBound;

        public WorldReadyPhase Phase { get; private set; } = WorldReadyPhase.None;

        public uint Epoch { get; private set; }

        public event Action<WorldReadyPhase> PhaseChanged;

        protected override void OnStart()
        {
            base.OnStart();
            TryBindPersistence();
        }

        protected override void OnDestroyed()
        {
            if (_persistenceBound && SubSystems.TryGet(out PersistenceSubSystem persistence))
            {
                persistence.OnAfterRestore -= HandleAfterRestore;
            }

            base.OnDestroyed();
        }

        private void TryBindPersistence()
        {
            if (_persistenceBound)
            {
                return;
            }

            if (!SubSystems.TryGet(out PersistenceSubSystem persistence))
            {
                return;
            }

            // OnBeforeRestore epoch reset is done via NotifyStationTemplateRestoreBeginning from
            // Persistence (hub may spawn after this DDOL SubSystem's OnStart).
            persistence.OnAfterRestore += HandleAfterRestore;
            _persistenceBound = true;
        }

        public bool IsReady(WorldReadyPhase phase)
        {
            if (phase == WorldReadyPhase.None)
            {
                return true;
            }

            if (phase == WorldReadyPhase.WorldReady)
            {
                return Phase == WorldReadyPhase.WorldReady;
            }

            return HasGate(phase);
        }

        public async UniTask WaitUntilAsync(WorldReadyPhase phase, CancellationToken cancellationToken = default)
        {
            TryBindPersistence();

            if (IsReady(phase))
            {
                return;
            }

            await UniTask.WaitUntil(() => IsReady(phase), cancellationToken: cancellationToken);
        }

        public void NotifyTileMapLoaded() => SetGate(WorldReadyPhase.TileMapLoaded);

        public void NotifyAreasFlooded() => SetGate(WorldReadyPhase.AreasFlooded);

        public void NotifyElectricityReady() => SetGate(WorldReadyPhase.ElectricityReady);

        public void NotifyAtmosReady() => SetGate(WorldReadyPhase.AtmosReady);

        public void NotifyDisposalReady() => SetGate(WorldReadyPhase.DisposalReady);

        /// <summary>
        /// Called from <see cref="PersistenceSubSystem"/> at the start of a station template restore
        /// (in addition to event subscription, which may not be bound yet while Boot-only).
        /// </summary>
        public void NotifyStationTemplateRestoreBeginning() => ResetEpoch();

        /// <summary>
        /// Legacy map load (no PersistenceSubSystem) after area flood ends.
        /// </summary>
        public void NotifyLegacyMapLoadComplete()
        {
            NotifyTileMapLoaded();
            NotifyAreasFlooded();
        }

        private void HandleAfterRestore(PersistenceLayer layer)
        {
            if (layer != PersistenceLayer.StationTemplate)
            {
                return;
            }

            // Prefer Persistence's direct NotifyTileMapLoaded; keep as belt-and-suspenders when bound.
            NotifyTileMapLoaded();
        }

        private void ResetEpoch()
        {
            Epoch++;
            _gates = 0;
            Phase = WorldReadyPhase.None;
            PhaseChanged?.Invoke(Phase);
            Log.Information(this, "World readiness reset (epoch {epoch})", Logs.ServerOnly, Epoch);
        }

        private void SetGate(WorldReadyPhase gate)
        {
            TryBindPersistence();

            byte bit = GateBit(gate);
            if (bit == 0 || (_gates & bit) != 0)
            {
                return;
            }

            _gates |= bit;
            Log.Information(this, "World ready gate {gate} (epoch {epoch})", Logs.ServerOnly, gate, Epoch);
            RecomputePhase();
        }

        private void RecomputePhase()
        {
            WorldReadyPhase next = WorldReadyPhase.None;
            if (HasGate(WorldReadyPhase.TileMapLoaded))
            {
                next = WorldReadyPhase.TileMapLoaded;
            }

            if (HasGate(WorldReadyPhase.AreasFlooded))
            {
                next = WorldReadyPhase.AreasFlooded;
            }

            if (HasGate(WorldReadyPhase.ElectricityReady))
            {
                next = WorldReadyPhase.ElectricityReady;
            }

            if (HasGate(WorldReadyPhase.AtmosReady))
            {
                next = WorldReadyPhase.AtmosReady;
            }

            if (HasGate(WorldReadyPhase.DisposalReady))
            {
                next = WorldReadyPhase.DisposalReady;
            }

            if (HasGate(WorldReadyPhase.TileMapLoaded)
                && HasGate(WorldReadyPhase.AreasFlooded)
                && HasGate(WorldReadyPhase.ElectricityReady)
                && HasGate(WorldReadyPhase.AtmosReady)
                && HasGate(WorldReadyPhase.DisposalReady))
            {
                next = WorldReadyPhase.WorldReady;
            }

            if (Phase == next)
            {
                return;
            }

            Phase = next;
            PhaseChanged?.Invoke(Phase);
            if (Phase == WorldReadyPhase.WorldReady)
            {
                Log.Information(this, "World ready (epoch {epoch})", Logs.ServerOnly, Epoch);
            }
        }

        private bool HasGate(WorldReadyPhase gate) => (_gates & GateBit(gate)) != 0;

        private static byte GateBit(WorldReadyPhase gate) => gate switch
        {
            WorldReadyPhase.TileMapLoaded => 1 << 0,
            WorldReadyPhase.AreasFlooded => 1 << 1,
            WorldReadyPhase.ElectricityReady => 1 << 2,
            WorldReadyPhase.AtmosReady => 1 << 3,
            WorldReadyPhase.DisposalReady => 1 << 4,
            _ => 0,
        };
    }
}
