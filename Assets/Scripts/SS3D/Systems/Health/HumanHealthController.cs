using FishNet.Object;
using FishNet.Object.Synchronizing;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using System.Collections.Generic;
using System.Linq;
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
        private Entity _entity;
        private WoundVfx _woundVfx;
        private HealthAlertsView _healthAlertsView;

        [SyncVar(OnChange = nameof(SyncSnapshot))]
        private HealthSnapshot _snapshot = HealthSnapshot.Default;

        [SyncVar]
        private HealthDebugDetail _debugDetail;

        public HealthSnapshot Snapshot => _snapshot;

        public HealthDebugDetail DebugDetail => _debugDetail;

        public float GetStoredOrganFunction(OrganType type)
        {
            for (int i = 0; i < _organs.Count; i++)
            {
                if (_organs[i].Type == type)
                {
                    return _organs[i].FunctionPercent;
                }
            }

            return 100f;
        }

        protected override void OnStart()
        {
            base.OnStart();
            InitializeDefaults();
            _entity = GetComponent<Entity>();
            _woundVfx = GetComponent<WoundVfx>();
            if (_woundVfx == null)
            {
                _woundVfx = gameObject.AddComponent<WoundVfx>();
            }

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            List<HealthAlertsView> alertViews = ViewLocator.Get<HealthAlertsView>();
            _healthAlertsView = alertViews?.FirstOrDefault();
            if (_entity != null)
            {
                _entity.OnMindChanged += AssignAlertsViewToControllable;
                InitialAssignAlertsView();
            }

            _woundVfx?.ApplySnapshot(_snapshot);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            EnsureBuiltinOrgans();
            OrganSimulation.EnsureDefaultOrgans(_organs);
            PublishSnapshot();
        }

        protected override void OnDestroyed()
        {
            if (_entity != null)
            {
                _entity.OnMindChanged -= AssignAlertsViewToControllable;
            }

            _healthAlertsView?.UnassignViewFromPlayer(this);
            base.OnDestroyed();
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
            RefreshZoneDerivedState(ref state);
            state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);
            _zones[index] = state;
            OrganSimulation.ApplyZoneDamageToOrgans(zone, brute, burn, _organs);

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

            RefreshZoneDerivedState(ref state);
            if (!stopBleeding)
            {
                state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);
            }

            _zones[index] = state;

            PublishSnapshot();
        }

        [Server]
        public void RestoreSystemicPools()
        {
            _pools = SystemicPools.Default;
            PublishSnapshot();
        }

        [Server]
        public void RestoreOrgans()
        {
            for (int i = 0; i < _organs.Count; i++)
            {
                OrganState organ = _organs[i];
                organ.FunctionPercent = 100f;
                organ.IsCritical = false;
                _organs[i] = organ;
            }

            PublishSnapshot();
        }

        [Server]
        public void TickHealth(float atmosphereO2 = 1f)
        {
            OrganSimulation.TickOrganFunction(_pools, _organs);
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

        private static void RefreshZoneDerivedState(ref ZoneDamageState state)
        {
            state.Severity = HealthSimulation.ResolveZoneSeverity(state.Brute, state.Burn);
            state.IsDisabled = state.Severity >= WoundSeverity.Disabled;
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
            _debugDetail = HealthDebugDetail.FromStates(_zones, _organs);
        }

        private void SyncSnapshot(HealthSnapshot oldValue, HealthSnapshot newValue, bool asServer)
        {
            _woundVfx?.ApplySnapshot(newValue);

            if (_healthAlertsView != null && _entity != null && _entity.Mind != null && _entity.Mind.IsOwner)
            {
                _healthAlertsView.Refresh();
            }
        }

        [Client]
        private void AssignAlertsViewToControllable(Mind mind)
        {
            if (_healthAlertsView == null)
            {
                return;
            }

            if (mind == null || !mind.IsOwner)
            {
                _healthAlertsView.UnassignViewFromPlayer(this);
            }
            else
            {
                _healthAlertsView.AssignViewToPlayer(this);
            }
        }

        [Client]
        private void InitialAssignAlertsView()
        {
            if (_entity.Mind != null && _entity.Mind.IsOwner)
            {
                AssignAlertsViewToControllable(_entity.Mind);
            }
        }

        [Server]
        private void EnsureBuiltinOrgans()
        {
            AttachOrganIfMissing("HumanBrain", OrganType.Brain);
            AttachOrganIfMissing("HumanHeart", OrganType.Heart);
            AttachOrganIfMissing("HumanLungLeft", OrganType.LeftLung);
            AttachOrganIfMissing("HumanLungRight", OrganType.RightLung);
            AttachOrganIfMissing("HumanLiver", OrganType.Liver);
        }

        [Server]
        private void AttachOrganIfMissing(string objectName, OrganType type)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != objectName)
                {
                    continue;
                }

                OrganInstance organ = transforms[i].GetComponent<OrganInstance>();
                if (organ == null)
                {
                    organ = transforms[i].gameObject.AddComponent<OrganInstance>();
                }

                organ.Configure(type);
                return;
            }
        }
    }
}
