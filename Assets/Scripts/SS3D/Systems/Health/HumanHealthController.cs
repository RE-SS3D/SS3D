using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.ScreenEffects;
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
        private HumanAnatomyController _anatomy;
        private HealthAlertsView _healthAlertsView;
        private Ragdoll _ragdoll;
        private bool _deathTriggered;
        private bool _unconsciousRagdollActive;
        private bool _drivingLocalScreenEffects;

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

        /// <summary>
        /// Relative brute damage (0..1) for a body zone, normalised against the disabled threshold.
        /// Replaces the legacy FootBodyPart.RelativeDamage the gait/limp presentation used to read.
        /// </summary>
        public float GetZoneBruteFraction(BodyZone zone)
        {
            int index = (int)zone;
            if (index < 0 || index >= _zones.Length)
            {
                return 0f;
            }

            float fraction = _zones[index].Brute / HealthConstants.DisabledThreshold;
            return fraction < 0f ? 0f : (fraction > 1f ? 1f : fraction);
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

            _anatomy = GetComponent<HumanAnatomyController>();
            if (_anatomy == null)
            {
                _anatomy = gameObject.AddComponent<HumanAnatomyController>();
            }

            _anatomy.Initialize(this);

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
            ApplySeveranceVisualsFromSnapshot(_snapshot);
            ApplyScreenEffectsFromSnapshot(_snapshot);
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
            ClearScreenEffectsIfDriving();

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
        public void ApplyDamage(BodyZone zone, MeleeDamagePacket packet)
        {
            ApplyDamage(zone, packet.Brute, packet.Burn);

            if (packet.CanSever)
            {
                TrySeverZone(zone);
            }
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
            HealthSimulation.RefreshZoneDerivedState(ref state);
            if (!state.IsSevered)
            {
                state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);
            }

            _zones[index] = state;
            OrganSimulation.ApplyZoneDamageToOrgans(zone, brute, burn, _organs);

            PublishSnapshot();

            if (brute + burn > 0f && Owner.IsValid)
            {
                RpcHitFlash(Owner);
            }
        }

        [Server]
        public void ApplyTreatment(BodyZone zone, float bruteHeal = 0f, float burnHeal = 0f, bool stopBleeding = false, bool applySplint = false)
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

            if (applySplint && !state.IsSevered)
            {
                state.IsSplinted = true;
            }

            HealthSimulation.RefreshZoneDerivedState(ref state);
            if (!stopBleeding && !state.IsSevered)
            {
                state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);
            }

            _zones[index] = state;

            PublishSnapshot();
        }

        [Server]
        public void ApplyBloodTransfusion(float bloodRestore = HealthConstants.TransfusionBloodRestore)
        {
            _pools = HealthSimulation.ApplyBloodTransfusion(_pools, bloodRestore);
            PublishSnapshot();
        }

        [Server]
        public void ApplyOxyRelief(float oxyRelief = HealthConstants.OxygenTankOxyRelief)
        {
            _pools = HealthSimulation.ApplyOxyRelief(_pools, oxyRelief);
            PublishSnapshot();
        }

        /// <summary>
        /// Stamina push-past-empty bridge (health Phase 7a / Documents/design/stamina.md §3).
        /// </summary>
        [Server]
        public void ApplyOxyDebt(float oxyDebtGain)
        {
            if (oxyDebtGain <= 0f)
            {
                return;
            }

            _pools = HealthSimulation.ApplyOxyDebt(_pools, oxyDebtGain);
            PublishSnapshot();
        }

        [Server]
        public void ApplyAntitoxin(float toxinReduction = HealthConstants.AntitoxinReduction)
        {
            _pools = HealthSimulation.ApplyAntitoxin(_pools, toxinReduction);
            PublishSnapshot();
        }

        [Server]
        public void ApplyCpr()
        {
            _pools = HealthSimulation.ApplyOxyRelief(_pools, HealthConstants.CprOxyRelief);
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
        public DefibrillatorOutcome TryDefibrillate(BodyZone zone)
        {
            DefibrillatorOutcome outcome = HealthSimulation.ApplyDefibrillation(zone, _organs, _zones, out float burnApplied);
            if (burnApplied > 0f)
            {
                int chestIndex = (int)BodyZone.Chest;
                ZoneDamageState chest = _zones[chestIndex];
                chest.BleedingRate = HealthSimulation.BleedingRateForSeverity(chest.Severity);
                _zones[chestIndex] = chest;
            }

            PublishSnapshot();
            return outcome;
        }

        [Server]
        public bool TrySeverZone(BodyZone zone, bool force = false)
        {
            if (!HealthSimulation.IsSeverableZone(zone))
            {
                return false;
            }

            int index = (int)zone;
            if (index < 0 || index >= _zones.Length)
            {
                return false;
            }

            ZoneDamageState state = _zones[index];
            if (state.IsSevered)
            {
                return false;
            }

            if (!force && state.Severity < WoundSeverity.Disabled)
            {
                return false;
            }

            HealthSimulation.ApplySeverance(ref state);
            _zones[index] = state;

            _anatomy.ExecuteServerSeverance(zone);
            RpcApplySeveranceVisuals(zone);
            PublishSnapshot();
            return true;
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcApplySeveranceVisuals(BodyZone zone)
        {
            _anatomy?.ApplyVisualSeverance(zone);
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
            PublishSnapshot();

            if (state == HealthState.Dead)
            {
                TriggerDeath();
            }
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsServer || _deathTriggered)
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
            if (_deathTriggered)
            {
                return;
            }

            _deathTriggered = true;

            if (TryGetComponent(out Human human))
            {
                human.Kill();
            }
        }

        [Server]
        private void PublishSnapshot()
        {
            HealthSnapshot snapshot = HealthSimulation.BuildSnapshot(_pools, _zones, _organs);
            _snapshot = snapshot;
            _debugDetail = HealthDebugDetail.FromStates(_zones, _organs);

            // Do not rely on SyncVar OnChange for this — FishNet may not invoke it on the
            // server when assigning the snapshot, which left unconscious players walking.
            ApplyConsciousnessRagdoll(snapshot);
            // Same host gap for local screen overlays.
            ApplyScreenEffectsFromSnapshot(snapshot);
        }

        private void SyncSnapshot(HealthSnapshot oldValue, HealthSnapshot newValue, bool asServer)
        {
            _woundVfx?.ApplySnapshot(newValue);
            ApplySeveranceVisualsFromSnapshot(newValue);
            ApplyScreenEffectsFromSnapshot(newValue);

            if (_healthAlertsView != null && IsLocalOwnerMind())
            {
                _healthAlertsView.Refresh();
            }
        }

        [TargetRpc(RunLocally = true)]
        private void RpcHitFlash(NetworkConnection target)
        {
            SubSystems.Get<ScreenEffectsSubSystem>()?.TriggerHitFlash();
        }

        private void ApplyScreenEffectsFromSnapshot(HealthSnapshot snapshot)
        {
            if (!IsLocalOwnerMind())
            {
                return;
            }

            _drivingLocalScreenEffects = true;
            ScreenEffectsSubSystem effects = SubSystems.Get<ScreenEffectsSubSystem>();
            if (snapshot.State == HealthState.Dead)
            {
                HealthScreenEffectMapper.Clear(effects);
                return;
            }

            HealthScreenEffectMapper.Apply(snapshot, effects);
        }

        private void ClearScreenEffectsIfDriving()
        {
            if (!_drivingLocalScreenEffects)
            {
                return;
            }

            _drivingLocalScreenEffects = false;
            HealthScreenEffectMapper.Clear(SubSystems.Get<ScreenEffectsSubSystem>());
        }

        private bool IsLocalOwnerMind()
        {
            return _entity != null && _entity.Mind != null
                && _entity.Mind != Mind.Empty && _entity.Mind.IsOwner;
        }

        /// <summary>
        /// Unconscious / cardiac-arrest characters drop into a recoverable ragdoll; waking stands them up.
        /// Death uses <see cref="Ragdoll.ServerDeathRagdoll"/> separately and is ignored here.
        /// </summary>
        [Server]
        private void ApplyConsciousnessRagdoll(HealthSnapshot snapshot)
        {
            if (_deathTriggered || snapshot.State == HealthState.Dead)
            {
                return;
            }

            if (_ragdoll == null)
            {
                _ragdoll = GetComponent<Ragdoll>();
            }

            if (_ragdoll == null)
            {
                return;
            }

            // Cardiac arrest keeps IsConscious true until brain drains ≤10%; still collapse immediately.
            bool shouldCollapse = !snapshot.IsConscious || snapshot.IsCardiacArrest;
            if (shouldCollapse)
            {
                _ragdoll.ServerKnockdownTimeless();
                RpcSetConsciousnessCollapsed(true);
                _unconsciousRagdollActive = true;
                return;
            }

            if (!_unconsciousRagdollActive)
            {
                return;
            }

            _unconsciousRagdollActive = false;
            if (_ragdoll.IsKnockedDown)
            {
                _ragdoll.ServerRecover();
            }

            RpcSetConsciousnessCollapsed(false);
        }

        /// <summary>
        /// Mirrors death's observer reinforce — host/client must apply collapse locally; SyncVar
        /// knockdown alone left upright walk-cycle corpses.
        /// </summary>
        [ObserversRpc(RunLocally = true)]
        private void RpcSetConsciousnessCollapsed(bool collapsed)
        {
            if (!TryGetComponent(out Ragdoll ragdoll))
            {
                return;
            }

            if (collapsed)
            {
                ragdoll.ApplyCollapseVisuals();
                return;
            }

            // Recover SyncVar drives BonesReset/StandUp on server; observers just clear suppress.
            if (TryGetComponent(out AnimationOrchestrator orchestrator))
            {
                orchestrator.SetPosingSuppressed(false);
            }
        }

        private void ApplySeveranceVisualsFromSnapshot(HealthSnapshot snapshot)
        {
            if (_anatomy == null)
            {
                return;
            }

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                if (snapshot.IsZoneSevered((BodyZone)i))
                {
                    _anatomy.ApplyVisualSeverance((BodyZone)i);
                }
            }
        }

        [Client]
        private void AssignAlertsViewToControllable(Mind mind)
        {
            if (mind == null || !mind.IsOwner)
            {
                _healthAlertsView?.UnassignViewFromPlayer(this);
                ClearScreenEffectsIfDriving();
                return;
            }

            _healthAlertsView?.AssignViewToPlayer(this);
            ApplyScreenEffectsFromSnapshot(_snapshot);
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
