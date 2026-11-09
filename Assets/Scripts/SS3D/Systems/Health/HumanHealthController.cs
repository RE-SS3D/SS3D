using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Audio;
using AudioType = SS3D.Systems.Audio.AudioType;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Tile;
using System;
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
        private Entity _entity;
        private WoundVfx _woundVfx;
        private HumanAnatomyController _anatomy;
        private Ragdoll _ragdoll;
        private HumanoidCombatController _combatController;
        private HumanInventory _inventory;
        private EvaGearController _evaGear;
        private bool _deathTriggered;
        private bool _healthCollapseActive;
        private bool _drivingLocalPresentation;
        private HealthEnvironmentState _environment = HealthEnvironmentState.SafeDefault;
        private HealthState _previousAudioHealthState = HealthState.Healthy;
        private bool _previousAudioVacuum;
        private float _nextScreamTime;

        [SyncVar(OnChange = nameof(SyncSnapshot))]
        private HealthSnapshot _snapshot = HealthSnapshot.Default;

        [SyncVar]
        private HealthDebugDetail _debugDetail;

        /// <summary>
        /// Raised whenever the synced snapshot is assigned — from SyncVar OnChange and from
        /// <see cref="PublishSnapshot"/> on the host (FishNet may skip OnChange for server assigns).
        /// Main HUD and other local-owner consumers subscribe here.
        /// </summary>
        public event Action<HealthSnapshot> SnapshotChanged;

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
            _inventory = GetComponent<HumanInventory>();
            _evaGear = GetComponent<EvaGearController>();
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
            if (_entity != null)
            {
                _entity.OnMindChanged += HandleMindChangedForLocalFeedback;
                InitialAssignLocalFeedback();
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
                _entity.OnMindChanged -= HandleMindChangedForLocalFeedback;
            }

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
            if (EntityInvincibility.IsInvincible(this))
            {
                return;
            }

            int index = (int)zone;
            if (index < 0 || index >= _zones.Length)
            {
                return;
            }

            (brute, burn) = ApplyArmorAbsorption(zone, brute, burn);

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

            // Dead / unminded corpses must not flash the ghost's screen — Owner may still be valid
            // briefly if SetMind(null) has not run yet; never TargetRpc after death latch.
            if (brute + burn > 0f
                && Owner.IsValid
                && !_deathTriggered
                && _snapshot.State != HealthState.Dead)
            {
                RpcHitFlash(Owner);
            }

            if (brute >= HealthConstants.BloodSprayMinBrute)
            {
                float intensity = Mathf.Clamp01(brute / HealthConstants.BloodSprayFullBrute);
                RpcBloodImpactBurst(zone, intensity);
                TryApplyHitFlinch(brute);
                PlayHitImpactSounds(intensity);
                TryPlayPainScream(brute);
            }
        }

        /// <summary>
        /// Standing flinch via <see cref="HumanoidCombatController.OnHitReceived"/> — only while locomotion.
        /// </summary>
        [Server]
        private void TryApplyHitFlinch(float brute)
        {
            if (_ragdoll == null)
            {
                _ragdoll = GetComponent<Ragdoll>();
            }

            if (_ragdoll != null && _ragdoll.Presentation != BodyPresentationState.Locomotion)
            {
                return;
            }

            if (_combatController == null)
            {
                _combatController = GetComponent<HumanoidCombatController>();
            }

            if (_combatController == null)
            {
                return;
            }

            float t = Mathf.Clamp01(brute / HealthConstants.BloodSprayFullBrute);
            // Keep stagger through most of the flinch clip so Additive Flinch weight can lerp out softly.
            float staggerSeconds = Mathf.Lerp(0.55f, 0.85f, t);
            _combatController.OnHitReceived(Vector3.zero, knockbackForce: 0f, staggerSeconds);
        }

        /// <summary>
        /// Runs incoming damage through every worn armor piece covering this zone before it reaches
        /// the limb model (Documents/design/armor.md §2). Pieces are layered outermost first — each
        /// one's absorption reduces the running remainder before the next piece sees it. Damage left
        /// after every layer punctures the outermost sealed piece covering the zone (armor.md §3).
        /// </summary>
        [Server]
        private (float Brute, float Burn) ApplyArmorAbsorption(BodyZone zone, float brute, float burn)
        {
            if (_inventory == null)
            {
                return (brute, burn);
            }

            SealedGearItemExtension outermostSeal = null;

            foreach (AttachedContainer container in _inventory.Containers
                .Where(worn => worn.Type.IsWornSlot())
                .OrderBy(worn => ArmorSimulation.LayerRank(worn.Type)))
            {
                foreach (Item item in container.Items)
                {
                    if (!item.TryGetComponent(out ArmorItemExtension armor) || !armor.Profile.CoveredZones.Contains(zone))
                    {
                        continue;
                    }

                    if (outermostSeal == null)
                    {
                        item.TryGetComponent(out outermostSeal);
                    }

                    (brute, burn) = armor.ServerAbsorb(brute, burn);
                }
            }

            if (outermostSeal != null && ArmorSimulation.ReachesLimb(brute, burn))
            {
                outermostSeal.ServerBreach();
            }

            return (brute, burn);
        }

        /// <summary>Whether an intact worn piece with <see cref="ArmorProfile.BlocksStun"/> covers this zone.</summary>
        [Server]
        public bool ServerZoneBlocksStun(BodyZone zone)
        {
            if (_inventory == null)
            {
                return false;
            }

            foreach (AttachedContainer container in _inventory.Containers.Where(worn => worn.Type.IsWornSlot()))
            {
                foreach (Item item in container.Items)
                {
                    if (item.TryGetComponent(out ArmorItemExtension armor)
                        && armor.Profile.BlocksStun
                        && armor.Integrity > 0f
                        && armor.Profile.CoveredZones.Contains(zone))
                    {
                        return true;
                    }
                }
            }

            return false;
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
        /// Forces the canonical brain-death trigger (Documents/design/health.md — death has
        /// exactly one trigger, brain function reaching zero) instead of ghosting the entity
        /// directly. Used by admin tooling so it doesn't bypass health state.
        /// </summary>
        [Server]
        public void ForceBrainDeath()
        {
            OrganSimulation.SetOrganFunction(_organs, OrganType.Brain, 0f);
            PublishSnapshot();
            TriggerDeath();
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
            if (!force && EntityInvincibility.IsInvincible(this))
            {
                return false;
            }

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
            RpcBloodImpactBurst(zone, 1f);
            PublishSnapshot();
            return true;
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcApplySeveranceVisuals(BodyZone zone)
        {
            _anatomy?.ApplyVisualSeverance(zone);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBloodImpactBurst(BodyZone zone, float intensity)
        {
            _woundVfx?.PlayImpactBurst(zone, intensity);
        }

        /// <summary>
        /// Positional flesh + blood SFX at the body (audio.md §3). Server-triggered so the pool
        /// fans out via ObserversRpc; parent null so StopAudioSource on the body cannot kill it.
        /// </summary>
        [Server]
        private void PlayHitImpactSounds(float intensity)
        {
            AudioSubSystem audio = SubSystems.Get<AudioSubSystem>();
            if (audio == null)
            {
                return;
            }

            Vector3 position = Transform.position;
            string[] flesh = CombatAudioTrackIds.FleshHit;
            string fleshId = flesh[UnityEngine.Random.Range(0, flesh.Length)];
            float pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            audio.PlayAudioSource(AudioType.Sfx, fleshId, position, null, false, 0.75f, pitch);

            string bloodId = intensity >= 0.65f ? HealthAudioTrackIds.Splat : HealthAudioTrackIds.Blood1;
            audio.PlayAudioSource(AudioType.Sfx, bloodId, position, null, false, 0.55f + 0.35f * intensity, pitch);
        }

        [Server]
        private void TryPlayPainScream(float brute)
        {
            if (brute < HealthConstants.ScreamMinBrute || Time.time < _nextScreamTime)
            {
                return;
            }

            // Dead / unconscious / collapsed bodies do not vocalize — flesh impact SFX still play.
            if (_deathTriggered
                || _snapshot.State == HealthState.Dead
                || !_snapshot.IsConscious)
            {
                return;
            }

            if (_ragdoll == null)
            {
                _ragdoll = GetComponent<Ragdoll>();
            }

            if (_ragdoll != null && _ragdoll.Presentation != BodyPresentationState.Locomotion)
            {
                return;
            }

            _nextScreamTime = Time.time + HealthConstants.ScreamCooldownSeconds;
            SubSystems.Get<AudioSubSystem>()?.PlayAudioSource(
                AudioType.Sfx,
                HealthAudioTrackIds.MaleScream,
                Transform.position,
                null,
                false,
                0.85f,
                UnityEngine.Random.Range(0.95f, 1.05f));
        }

        /// <summary>
        /// Gasp/choke on entering critical or vacuum — positional so nearby players hear it.
        /// </summary>
        [Server]
        private void TryPlayHealthStateAudio(HealthSnapshot snapshot)
        {
            AudioSubSystem audio = SubSystems.Get<AudioSubSystem>();
            if (audio == null)
            {
                _previousAudioHealthState = snapshot.State;
                _previousAudioVacuum = BreathingVacuum(snapshot.Environment);
                return;
            }

            bool enteredCritical = snapshot.State == HealthState.Critical
                && _previousAudioHealthState != HealthState.Critical
                && snapshot.IsConscious
                && !snapshot.IsCardiacArrest;

            bool enteredVacuum = BreathingVacuum(snapshot.Environment) && !_previousAudioVacuum;

            if (enteredCritical || enteredVacuum)
            {
                string clipId;
                if (enteredVacuum)
                {
                    string[] choke = HealthAudioTrackIds.Choke;
                    clipId = choke[UnityEngine.Random.Range(0, choke.Length)];
                }
                else
                {
                    clipId = HealthAudioTrackIds.MaleGasp;
                }

                audio.PlayAudioSource(
                    AudioType.Sfx,
                    clipId,
                    Transform.position,
                    null,
                    false,
                    0.8f,
                    UnityEngine.Random.Range(0.95f, 1.05f));
            }

            _previousAudioHealthState = snapshot.State;
            _previousAudioVacuum = BreathingVacuum(snapshot.Environment);
        }

        private static bool BreathingVacuum(HealthEnvironmentState env) => env.IsVacuum && !env.OnInternals;

        [Server]
        public void TickHealth(float atmosphereO2 = 1f, float toxinIntake = HealthConstants.BaseToxinIntake)
        {
            OrganSimulation.TickOrganFunction(_pools, _organs);
            _pools = HealthSimulation.TickPools(_pools, _zones, _organs, atmosphereO2, toxinIntake);

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
            TickHealthFromEnvironment();
        }

        [Server]
        private void TickHealthFromEnvironment()
        {
            if (HealthEnvironmentSettings.AtmosphericDamageDisabled)
            {
                _environment = HealthEnvironmentState.SafeDefault;
                TickHealth(atmosphereO2: 1f, toxinIntake: HealthConstants.BaseToxinIntake);
                return;
            }

            // Invincibility stops the harm, not the reading: its owner still sees what the air is doing.
            if (EntityInvincibility.IsInvincible(this))
            {
                _environment = ApplyInternals(ApplyWornSeal(SampleEnvironmentAtBody()));
                TickHealth(atmosphereO2: 1f, toxinIntake: HealthConstants.BaseToxinIntake);
                return;
            }

            _environment = ApplyInternals(ApplyWornSeal(SampleEnvironmentAtBody()));
            ApplyEnvironmentalExposure(_environment);
            ApplyBreathExchange(_environment);

            float toxinIntake = HealthEnvironmentExposure.ToxinIntakeFromPlasma(
                _environment.PlasmaMoleFraction);
            TickHealth(_environment.AtmosphereBreathability, toxinIntake);
        }

        /// <summary>
        /// Suit and helmet seal together; a missing or breached piece breaks the whole seal (armor.md §3).
        /// </summary>
        [Server]
        private HealthEnvironmentState ApplyWornSeal(HealthEnvironmentState env)
        {
            SealedGearItemExtension suit = FindWornSeal(ContainerType.ExoSuit);
            SealedGearItemExtension helmet = FindWornSeal(ContainerType.Head);

            env.IsBreached = (suit != null && suit.IsBreached) || (helmet != null && helmet.IsBreached);
            if (suit != null && helmet != null && !env.IsBreached)
            {
                env.IsSealed = true;
                env.HeatProtection = Mathf.Min(suit.HeatProtection, helmet.HeatProtection);
            }

            return env;
        }

        /// <summary>
        /// While internals are on, the breath comes from the tank and is drawn from it (armor.md §3).
        /// </summary>
        [Server]
        private HealthEnvironmentState ApplyInternals(HealthEnvironmentState env)
        {
            GasTankItemExtension tank = _evaGear != null ? _evaGear.ServerBreathingTank() : null;
            if (tank == null)
            {
                return env;
            }

            GasTankMix breathed = tank.Contents;
            tank.ServerDraw(HealthEnvironmentExposure.BreathOxygenMoles(1f, LungFunction01()));
            return HealthEnvironmentExposure.BreatheFromTank(env, breathed, tank.Fill);
        }

        private float LungFunction01()
        {
            return (HealthSimulation.GetOrganFunction(_organs, OrganType.LeftLung, _pools.BloodVolumeRatio)
                    + HealthSimulation.GetOrganFunction(_organs, OrganType.RightLung, _pools.BloodVolumeRatio))
                * 0.005f;
        }

        private SealedGearItemExtension FindWornSeal(ContainerType slot)
        {
            if (_inventory == null)
            {
                return null;
            }

            foreach (AttachedContainer container in _inventory.Containers)
            {
                if (container.Type != slot)
                {
                    continue;
                }

                foreach (Item item in container.Items)
                {
                    if (item.TryGetComponent(out SealedGearItemExtension seal))
                    {
                        return seal;
                    }
                }
            }

            return null;
        }

        [Server]
        private HealthEnvironmentState SampleEnvironmentAtBody()
        {
            if (!SubSystems.TryGet(out TileSubSystem tiles)
                || tiles.CurrentMap == null
                || !SubSystems.TryGet(out AtmosSubSystem atmos)
                || atmos.Simulation == null)
            {
                return HealthEnvironmentState.SafeDefault;
            }

            TileCoord coord = tiles.QueryService.WorldToTile(Transform.position, tiles.CurrentMap.MapId);
            AtmosSimulation simulation = atmos.Simulation;

            HumanoidSupportState support = HumanoidSpaceSupport.GetSupportAt(Transform.position);
            if (support == HumanoidSupportState.Unknown)
            {
                // Map/AOI not ready — same spirit as missing Tile/Atmos above.
                return HealthEnvironmentState.SafeDefault;
            }

            // Off the atmos grid (past chunk extents) or no plenum: open space stays vacuum.
            if (support == HumanoidSupportState.Unsupported)
            {
                if (simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo vacuumInfo))
                {
                    return HealthEnvironmentExposure.FromVacuumCell(
                        vacuumInfo.Temperature,
                        vacuumInfo.BurnIntensity);
                }

                return HealthEnvironmentExposure.FromVacuumCell(
                    AtmosConstants.SpaceTemperature,
                    burnIntensity: 0f);
            }

            if (!simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info))
            {
                // Floor present but atmos cell missing (init gap) — do not treat as space.
                return HealthEnvironmentState.SafeDefault;
            }

            if (info.State == AtmosCellState.Vacuum)
            {
                return HealthEnvironmentExposure.FromVacuumCell(info.Temperature, info.BurnIntensity);
            }

            if (AtmosAreaSampler.TrySampleTile(coord, simulation, out AtmosAreaSample sample))
            {
                return HealthEnvironmentExposure.FromTileSample(sample, isVacuum: false, info.BurnIntensity);
            }

            // Empty / zero-mole cell that isn't flagged vacuum yet — treat as unbreathable.
            return HealthEnvironmentExposure.FromVacuumCell(info.Temperature, info.BurnIntensity);
        }

        [Server]
        private void ApplyEnvironmentalExposure(HealthEnvironmentState env)
        {
            // Heat / cold / fire: surface burn across the whole body (slow per zone).
            float burn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                env.TemperatureKelvin,
                env.BurnIntensity,
                env.IsSealed,
                env.HeatProtection);
            if (burn > 0f)
            {
                for (int zone = 0; zone < _zones.Length; zone++)
                {
                    ApplyEnvironmentalZoneBurn((BodyZone)zone, burn);
                }
            }

            // Pressure extremes: lung barotrauma — not chest burn.
            float lungDamage = HealthEnvironmentExposure.PressureLungDamage(
                env.PressureKpa,
                env.IsVacuum,
                env.IsSealed);
            if (lungDamage > 0f)
            {
                OrganSimulation.ApplyLungDamage(_organs, lungDamage);
            }
        }

        [Server]
        private void ApplyEnvironmentalZoneBurn(BodyZone zone, float burn)
        {
            int index = (int)zone;
            if (index < 0 || index >= _zones.Length || burn <= 0f)
            {
                return;
            }

            ZoneDamageState state = _zones[index];
            if (state.IsSevered)
            {
                return;
            }

            state.Burn += burn;
            HealthSimulation.RefreshZoneDerivedState(ref state);
            state.BleedingRate = HealthSimulation.BleedingRateForSeverity(state.Severity);

            _zones[index] = state;
            // Temp/fire burn is dermal — do not cascade chest burn into heart/lung organ
            // damage here; pressure owns lung trauma via ApplyLungDamage.
        }

        [Server]
        private void ApplyBreathExchange(HealthEnvironmentState env)
        {
            // Internals breathe from the tank and exhale nothing into the room, so atmos cells stay asleep.
            if (!env.HasSample || env.IsVacuum || env.OnInternals || env.AtmosphereBreathability <= 0f)
            {
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tiles)
                || tiles.CurrentMap == null
                || !SubSystems.TryGet(out AtmosSubSystem atmos)
                || atmos.Simulation == null)
            {
                return;
            }

            float request = HealthEnvironmentExposure.BreathOxygenMoles(
                env.AtmosphereBreathability,
                LungFunction01());
            if (request <= 0f)
            {
                return;
            }

            TileCoord coord = tiles.QueryService.WorldToTile(Transform.position, tiles.CurrentMap.MapId);
            AtmosSimulation simulation = atmos.Simulation;
            if (!simulation.TryRemoveMoles(
                    coord,
                    AtmosConstants.Oxygen,
                    request,
                    out float removed,
                    out float sourceTemperature)
                || removed <= 0f)
            {
                return;
            }

            simulation.TryAddMolesAtTemperature(
                coord,
                AtmosConstants.CarbonDioxide,
                removed,
                sourceTemperature);
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
            snapshot.Environment = _environment;
            bool lostArmControl = _snapshot.CanUseArms && !snapshot.CanUseArms;
            _snapshot = snapshot;
            _debugDetail = HealthDebugDetail.FromStates(_zones, _organs);

            if (lostArmControl && TryGetComponent(out Hands hands))
            {
                foreach (Hand hand in hands.PlayerHands)
                {
                    if (hand == null || hand.Container == null || hand.IsEmpty())
                        continue;
                    hand.ItemInHand.GiveOwnership(null);
                    hand.Container.Dump();
                }
            }

            // Do not rely on SyncVar OnChange for this — FishNet may not invoke it on the
            // server when assigning the snapshot, which left unconscious players walking.
            ApplyBodyPresentationIntent(snapshot);
            // Same host gap for local screen overlays and HUD alert consumers.
            ApplyScreenEffectsFromSnapshot(snapshot);
            TryPlayHealthStateAudio(snapshot);
            SnapshotChanged?.Invoke(snapshot);
        }

        private void SyncSnapshot(HealthSnapshot oldValue, HealthSnapshot newValue, bool asServer)
        {
            _woundVfx?.ApplySnapshot(newValue);
            ApplySeveranceVisualsFromSnapshot(newValue);
            ApplyScreenEffectsFromSnapshot(newValue);
            SnapshotChanged?.Invoke(newValue);
        }

        [TargetRpc(RunLocally = true)]
        private void RpcHitFlash(NetworkConnection target)
        {
            // Belt-and-suspenders: only flash while this body still has a local living mind.
            if (!IsLocalOwnerMind() || _deathTriggered || _snapshot.State == HealthState.Dead)
            {
                return;
            }

            SubSystems.Get<ScreenEffectsSubSystem>()?.TriggerHitFlash();
        }

        private void ApplyScreenEffectsFromSnapshot(HealthSnapshot snapshot)
        {
            if (!IsLocalOwnerMind())
            {
                return;
            }

            _drivingLocalPresentation = true;
            ScreenEffectsSubSystem effects = SubSystems.Get<ScreenEffectsSubSystem>();
            PersonalAudioSubSystem personalAudio = SubSystems.Get<PersonalAudioSubSystem>();
            if (snapshot.State == HealthState.Dead)
            {
                HealthScreenEffectMapper.Clear(effects);
                AtmosScreenEffectMapper.Clear(effects);
                HealthPersonalAudioMapper.Clear(personalAudio);
                return;
            }

            HealthScreenEffectMapper.Apply(snapshot, effects);
            AtmosScreenEffectMapper.Apply(snapshot.Environment, effects);
            HealthPersonalAudioMapper.Apply(snapshot, personalAudio);
        }

        private void ClearScreenEffectsIfDriving()
        {
            if (!_drivingLocalPresentation)
            {
                return;
            }

            _drivingLocalPresentation = false;
            ScreenEffectsSubSystem effects = SubSystems.Get<ScreenEffectsSubSystem>();
            HealthScreenEffectMapper.Clear(effects);
            AtmosScreenEffectMapper.Clear(effects);
            HealthPersonalAudioMapper.Clear(SubSystems.Get<PersonalAudioSubSystem>());
        }

        private bool IsLocalOwnerMind()
        {
            return _entity != null && _entity.Mind != null
                && _entity.Mind != Mind.Empty && _entity.Mind.IsOwner;
        }

        /// <summary>
        /// Writes presentation intent to <see cref="Ragdoll"/> only. Death is owned by
        /// <see cref="Human.Kill"/> → <see cref="Ragdoll.ServerDeathRagdoll"/> and is ignored here.
        /// </summary>
        [Server]
        private void ApplyBodyPresentationIntent(HealthSnapshot snapshot)
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

            BodyPresentationState intent = BodyPresentationIntent.FromSnapshot(snapshot);
            if (intent == BodyPresentationState.Dead)
            {
                return;
            }

            // Collapsed vs Locomotion only — never fight Kill()'s Dead write.
            // Latch health-owned collapses so we do not clear combat timed knockdowns.
            if (intent == BodyPresentationState.Collapsed)
            {
                if (_ragdoll.Presentation != BodyPresentationState.Collapsed)
                {
                    _ragdoll.ServerSetPresentation(BodyPresentationState.Collapsed);
                }

                _healthCollapseActive = true;
                return;
            }

            if (!_healthCollapseActive)
            {
                return;
            }

            _healthCollapseActive = false;
            if (_ragdoll.Presentation == BodyPresentationState.Collapsed)
            {
                _ragdoll.ServerSetPresentation(BodyPresentationState.Locomotion);
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
        private void HandleMindChangedForLocalFeedback(Mind mind)
        {
            if (mind == null || !mind.IsOwner)
            {
                ClearScreenEffectsIfDriving();
                return;
            }

            ApplyScreenEffectsFromSnapshot(_snapshot);
        }

        [Client]
        private void InitialAssignLocalFeedback()
        {
            if (_entity.Mind != null && _entity.Mind.IsOwner)
            {
                HandleMindChangedForLocalFeedback(_entity.Mind);
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
