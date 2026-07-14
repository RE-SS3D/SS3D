using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Server-authoritative health controller for humanoids. Owns zone damage, organs, and systemic pools.
    /// </summary>
    public class HumanHealthController : NetworkActor
    {
        private readonly ZoneDamageState[] _zones = new ZoneDamageState[HealthConstants.ZoneCount];
        private readonly List<OrganState> _organs = new();
        private readonly List<IHealthEffectModifier> _modifiers = new();

        private SystemicPools _pools = SystemicPools.Default;
        private float _tickTimer;

        [SyncVar(OnChange = nameof(SyncSnapshot))]
        private HealthSnapshot _snapshot = HealthSnapshot.Default;

        public HealthSnapshot Snapshot => _snapshot;

        protected override void OnStart()
        {
            base.OnStart();
            InitializeDefaults();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            PublishSnapshot();
        }

        private void InitializeDefaults()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i] = ZoneDamageState.Default;
            }

            _pools = SystemicPools.Default;
        }

        public void RegisterOrgan(OrganInstance organ)
        {
            if (_organs.Exists(o => o.Type == organ.Type))
            {
                return;
            }

            _organs.Add(OrganState.Default(organ.Type));
        }

        public void UnregisterOrgan(OrganInstance organ)
        {
            _organs.RemoveAll(o => o.Type == organ.Type);
        }

        public void RegisterModifier(IHealthEffectModifier modifier)
        {
            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
            }
        }

        public void UnregisterModifier(IHealthEffectModifier modifier)
        {
            _modifiers.Remove(modifier);
        }

        [Server]
        public void ApplyDamage(BodyZone zone, float brute, float burn)
        {
            int index = (int)zone;
            if (index < 0 || index >= _zones.Length)
            {
                return;
            }

            ZoneDamageState state = _zones[index];
            state.Brute += brute;
            state.Burn += burn;
            state.Severity = HealthSimulation.SeverityFromBrute(state.Brute);
            state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);
            state.IsDisabled = state.Severity >= WoundSeverity.Disabled;
            _zones[index] = state;

            PublishSnapshot();
        }

        [Server]
        public void ApplyTreatment(BodyZone zone, float bruteHeal = 0f, float burnHeal = 0f, bool stopBleeding = false)
        {
            int index = (int)zone;
            if (index < 0 || index >= _zones.Length)
            {
                return;
            }

            ZoneDamageState state = _zones[index];
            state.Brute = Mathf.Max(0f, state.Brute - bruteHeal);
            state.Burn = Mathf.Max(0f, state.Burn - burnHeal);

            if (stopBleeding)
            {
                state.BleedingRate = 0f;
            }

            state.Severity = HealthSimulation.SeverityFromBrute(state.Brute);
            state.IsDisabled = state.Severity >= WoundSeverity.Disabled;
            _zones[index] = state;

            PublishSnapshot();
        }

        [Server]
        public void TickHealth(float atmosphereO2 = 1f)
        {
            _pools = HealthSimulation.TickPools(_pools, _zones, _organs, atmosphereO2);

            for (int i = 0; i < _modifiers.Count; i++)
            {
                _modifiers[i].ApplyTick(ref _pools, _organs.ToArray());
            }

            HealthState state = HealthSimulation.EvaluateHealthState(_pools, _organs);
            if (state == HealthState.Dead)
            {
                TriggerDeath();
            }

            PublishSnapshot();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsServer)
            {
                return;
            }

            _tickTimer += updateEvent.DeltaTime;
            if (_tickTimer < HealthConstants.TickIntervalSeconds)
            {
                return;
            }

            _tickTimer = 0f;
            TickHealth();
        }

        [Server]
        private void TriggerDeath()
        {
            if (TryGetComponent(out Human human))
            {
                human.Kill();
            }
        }

        [Server]
        private void PublishSnapshot()
        {
            _snapshot = HealthSimulation.BuildSnapshot(_pools, _zones, _organs);
        }

        private void SyncSnapshot(HealthSnapshot oldValue, HealthSnapshot newValue, bool asServer)
        {
        }
    }
}
