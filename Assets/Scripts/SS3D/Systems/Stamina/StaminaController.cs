using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Audio;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Stamina
{
    /// <summary>
    /// Phase 7a-core stamina: health-modulated regen, carried-weight encumbrance, sprint drain,
    /// push-past-empty → oxy debt. No permanent bar (Documents/design/stamina.md).
    /// </summary>
    public class StaminaController : NetworkActor, IHealthEffectModifier
    {
        /// <summary>Carried weight at which max/regen are halved (placeholder balancing).</summary>
        private const float ReferenceEncumbranceWeight = 40f;

        /// <summary>Oxy debt added per unit of stamina overdraw while pushing past empty.</summary>
        private const float OverdrawOxyDebtScale = 0.02f;

        [SerializeField] private HumanoidController _player;
        [SerializeField] private Entity _entity;

        [SyncVar(OnChange = nameof(SyncCurrentStamina))] private float _currentStamina;
        [SyncVar] private float _exertionPenalty;

        private IStamina _stamina;
        private HumanHealthController _health;
        private HumanInventory _inventory;
        private float _pendingOxyDebt;

        /// <summary>Owner-side sprint drain batched into one RPC per <see cref="DrainSendInterval"/> (was one per frame).</summary>
        private float _pendingSprintDrain;
        private float _sprintDrainSendTimer;
        private const float DrainSendInterval = 0.2f;

        /// <summary>Server: seconds since the last sprint drain arrived; regen waits <see cref="SprintExertion.RegenDelayAfterSprint"/>.</summary>
        private float _secondsSinceSprintDrain = float.MaxValue;

        public float CurrentStamina => _currentStamina;

        /// <summary>0 rested .. 1 exhausted. Synced for client movement.</summary>
        public float ExertionPenalty => IsServerOnly && _stamina != null ? _stamina.ExertionPenalty : _exertionPenalty;

        public bool CanCommenceInteraction => _stamina?.CanCommenceInteraction ?? true;

        public bool CanContinueInteraction => _stamina?.CanContinueInteraction ?? true;

        protected override void OnStart()
        {
            base.OnStart();
            _health = GetComponent<HumanHealthController>();
            if (_health == null)
            {
                _health = GetComponentInParent<HumanHealthController>();
            }

            _inventory = GetComponent<HumanInventory>();
            if (_inventory == null)
            {
                _inventory = GetComponentInParent<HumanInventory>();
            }
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _stamina = StaminaFactory.Create();
            RefreshModifiers();
            _currentStamina = _stamina.Current;
            _exertionPenalty = _stamina.ExertionPenalty;
            _health?.RegisterModifier(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Movement is still client-authoritative for sprint drain signalling.
            SubscribeToEvents();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsServer || _stamina == null)
            {
                return;
            }

            RefreshModifiers();
            _secondsSinceSprintDrain += updateEvent.DeltaTime;
            if (SprintExertion.ShouldRegenerate(_secondsSinceSprintDrain))
            {
                _stamina.RechargeStamina(updateEvent.DeltaTime);
            }

            PublishState();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
            _health?.UnregisterModifier(this);
        }

        private void SubscribeToEvents()
        {
            if (_player != null)
            {
                _player.OnSpeedChangeEvent += DepleteStamina;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_player != null)
            {
                _player.OnSpeedChangeEvent -= DepleteStamina;
            }
        }

        [Server]
        public void ServerDepleteStamina(float amountToDeplete)
        {
            if (_stamina == null)
            {
                return;
            }

            _stamina.ConsumeStamina(amountToDeplete);
            ApplyOverdrawOxyDebt();
            PublishState();
        }

        private void DepleteStamina(float gaitSpeed)
        {
            if (!IsOwner || _player == null)
            {
                return;
            }

            float fraction = SprintExertion.DrainFraction(gaitSpeed, _player.WalkAnimatorValue, _player.RunAnimatorValue);
            _pendingSprintDrain += fraction * SprintExertion.SprintDrainPerSecond * Time.deltaTime;
            _sprintDrainSendTimer += Time.deltaTime;

            // Flush on the interval, or at once when the sprint ends so the server stops draining promptly.
            if (_pendingSprintDrain > 0f && (_sprintDrainSendTimer >= DrainSendInterval || fraction <= 0f))
            {
                CmdDepleteStaminaScaled(_pendingSprintDrain);
                _pendingSprintDrain = 0f;
                _sprintDrainSendTimer = 0f;
            }
        }

        [ServerRpc]
        private void CmdDepleteStaminaScaled(float amountToDeplete)
        {
            if (_stamina == null || amountToDeplete <= 0f)
            {
                return;
            }

            _secondsSinceSprintDrain = 0f;
            _stamina.ConsumeStamina(amountToDeplete);
            ApplyOverdrawOxyDebt();
            PublishState();
        }

        /// <summary>
        /// Accumulates overdraw as a pending health-effect delta instead of calling
        /// HumanHealthController directly, so it flows through the IHealthEffectModifier
        /// seam the health plan designates for stamina overdraw (health_implementation_plan.md).
        /// </summary>
        [Server]
        private void ApplyOverdrawOxyDebt()
        {
            float overdraw = _stamina.LastOverdraw;
            if (overdraw <= 0f || _health == null)
            {
                return;
            }

            _pendingOxyDebt += overdraw * OverdrawOxyDebtScale;
        }

        /// <inheritdoc/>
        [Server]
        public void ApplyTick(ref SystemicPools pools, OrganState[] organs)
        {
            if (_pendingOxyDebt <= 0f)
            {
                return;
            }

            pools = HealthSimulation.ApplyOxyDebt(pools, _pendingOxyDebt);
            _pendingOxyDebt = 0f;
        }

        [Server]
        private void RefreshModifiers()
        {
            if (_stamina == null)
            {
                return;
            }

            float heart = 1f;
            float lungs = 1f;
            float blood = 1f;
            if (_health != null)
            {
                HealthSnapshot snapshot = _health.Snapshot;
                heart = Mathf.Clamp01(snapshot.HeartFunctionPercent / 100f);
                lungs = Mathf.Clamp01(
                    (_health.GetStoredOrganFunction(OrganType.LeftLung)
                     + _health.GetStoredOrganFunction(OrganType.RightLung)) / 200f);
                blood = Mathf.Clamp01(snapshot.Pools.BloodVolumeRatio);
            }

            float healthRegen = Mathf.Clamp01((heart + lungs) * 0.5f * blood);

            float carried = _inventory != null ? _inventory.CarriedWeight : 0f;
            float encumbrance = Mathf.Clamp01(carried / ReferenceEncumbranceWeight);
            float weightMaxScale = Mathf.Lerp(1f, 0.5f, encumbrance);
            float weightRegenScale = Mathf.Lerp(1f, 0.4f, encumbrance);

            _stamina.ApplyModifiers(weightMaxScale, healthRegen * weightRegenScale);
        }

        [Server]
        private void PublishState()
        {
            _currentStamina = _stamina.Current;
            _exertionPenalty = _stamina.ExertionPenalty;
        }

        private void SyncCurrentStamina(float old, float value, bool asServer)
        {
            if (!IsOwner)
            {
                return;
            }

            StaminaPersonalAudioMapper.Apply(value, SubSystems.Get<PersonalAudioSubSystem>());
        }
    }
}
