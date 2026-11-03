using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;
using System;
using System.Text;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Single writer to the Unity Animator for humanoids.
    /// Maps body state snapshots to animator parameters and layer weights.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationOrchestrator : Actor
    {
        public event Action<AnimationTriggerId> OnTriggerFired;

        [SerializeField] private HumanoidController _movementController;
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private Animator _animator;
        /// <summary>
        /// Remote-only smoothing of the replicated Speed SyncVar (arrives in tick-sized steps).
        /// The owner's controller already ramps locomotion at M1m pace, so the owner applies it directly.
        /// </summary>
        [SerializeField] private float _remoteLocomotionLerp = 14f;
        /// <summary>Smoothing for the per-frame yaw-delta Turn parameter, which is noisy frame to frame.</summary>
        [SerializeField] private float _turnLerp = 10f;
        [SerializeField] private HumanoidIkController _ikController;

        private float _currentSpeed;
        private float _targetSpeed;
        private float _currentVelX;
        private float _currentVelZ;
        private float _currentTurn;
        private float _targetVelX;
        private float _targetVelZ;
        private float _targetTurn;
        /// <summary>Set once the controller publishes VelX/VelZ; scalar Speed events then only carry stops.</summary>
        private bool _controllerPublishesVelocity;
        /// <summary>Other controllers (silicon, preview) may lack LocoRate; avoid the missing-parameter warning spam.</summary>
        private bool _hasLocoRateParameter;
        private BodyAnimationSnapshot _lastSnapshot = BodyAnimationSnapshot.Default;
        private AnimationTriggerId _lastConsumedTrigger = AnimationTriggerId.None;
        private byte _lastTriggerSequence;
        private bool _ownerPredictedAttack;
        private float _upperBodyWeight;
        private float _upperBodyWeightTarget;
        private float _additiveWeight;
        private float _additiveWeightTarget;
        private bool _posingSuppressed;
        private int _nextAttackVariant;
        private bool _hasAttackRateParameter;
        /// <summary>Time.time at which the active swing reaches its strike frame; AttackRate returns to 1 after.</summary>
        private float _swingStrikeTime = float.NegativeInfinity;

        /// <summary>Horizontal / downward / backhand cycle for melee AttackSwing.</summary>
        private static int MeleeSwingVariantCount => MeleeSwingAnimation.Variants.Length;

        /// <summary>Leg brute at/above this → stumble idle on injured blend; prefer base over arm additive.</summary>
        private const float StumbleLegThreshold = 0.65f;

        /// <summary>
        /// Soft Additive weight while Staggered so Additive Flinch (gut) fades in/out.
        /// Do not slam to 1 — Empty Additive is remapped to hurting idle and reads as a snap.
        /// </summary>
        private const float StaggerAdditiveWeight = 0.75f;

        /// <summary>
        /// Soft fade when entering/leaving Melee stance (Upper Body layer on/off).
        /// Swing clip lifetime is Animator exit-time owned — do not add swing duration constants here.
        /// </summary>
        [SerializeField] private float _upperBodyWeightLerp = 6f;

        [SerializeField] private float _additiveWeightLerp = 8f;

        private static readonly int RifleAimIdleStateHash = Animator.StringToHash("Rifle Aim Idle");
        private static readonly int FireRifleStateHash = Animator.StringToHash("Fire Rifle");
        private static readonly int ReloadRifleStateHash = Animator.StringToHash("Reload Rifle");
        private static readonly int HoldDefaultStateHash = Animator.StringToHash("Hold Default");

        private HumanoidCombatMode _lastAppliedCombatMode = HumanoidCombatMode.Peaceful;

        public Animator Animator => _animator;

        protected override void OnStart()
        {
            base.OnStart();
            if (_bodyStateMachine == null)
            {
                _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            }
            if (_movementController == null)
            {
                _movementController = GetComponent<HumanoidController>();
            }

            EnsureAnimator();

            LogMissingAnimatorParametersOnce();
            _hasLocoRateParameter = HasFloatParameter(Animations.Humanoid.LocoRate);
            _hasAttackRateParameter = HasFloatParameter(Animations.Humanoid.AttackRate);
            SubscribeToEvents();

            if (_bodyStateMachine != null)
            {
                _targetSpeed = _bodyStateMachine.Snapshot.MovementSpeed;
                _targetVelX = _bodyStateMachine.Snapshot.MovementVelX;
                _targetVelZ = _bodyStateMachine.Snapshot.MovementVelZ;
                ApplySnapshot(_bodyStateMachine.Snapshot);
            }
            else if (_animator != null)
            {
                _targetSpeed = 0f;
                _animator.SetFloat(Animations.Humanoid.MovementSpeed, 0f);
                _animator.SetFloat(Animations.Humanoid.VelX, 0f);
                _animator.SetFloat(Animations.Humanoid.VelZ, 0f);
                _animator.SetFloat(Animations.Humanoid.Turn, 0f);
            }
        }

        private bool HasFloatParameter(int hash)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureAnimator()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void LogMissingAnimatorParametersOnce()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                UnityEngine.Debug.LogError(
                    $"[AnimationOrchestrator] No runtimeAnimatorController on '{name}'.",
                    this);
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            bool Has(int hash)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].nameHash == hash)
                    {
                        return true;
                    }
                }

                return false;
            }

            (string Name, int Hash)[] required =
            {
                ("Speed", Animations.Humanoid.MovementSpeed),
                ("Floating", Animations.Humanoid.Floating),
                ("LimpSide", Animations.Humanoid.LimpSide),
                ("IsCrawling", Animations.Humanoid.IsCrawling),
                ("IsDragging", Animations.Humanoid.IsDragging),
                ("ArmHold", Animations.Humanoid.ArmHold),
                ("InjuredArmLeft", Animations.Humanoid.InjuredArmLeft),
                ("InjuredArmRight", Animations.Humanoid.InjuredArmRight),
                ("InjuredLeg", Animations.Humanoid.InjuredLeg),
                ("IsSeated", Animations.Humanoid.IsSeated),
                ("CombatMode", Animations.Humanoid.CombatMode),
                ("CombatStance", Animations.Humanoid.CombatStance),
                ("AimYaw", Animations.Humanoid.AimYaw),
                ("AimPitch", Animations.Humanoid.AimPitch),
                ("AttackVariant", Animations.Humanoid.AttackVariant),
                ("MirrorUpperBody", Animations.Humanoid.MirrorUpperBody),
                ("VelX", Animations.Humanoid.VelX),
                ("VelZ", Animations.Humanoid.VelZ),
                ("Turn", Animations.Humanoid.Turn),
            };

            StringBuilder missing = null;
            foreach ((string Name, int Hash) entry in required)
            {
                if (Has(entry.Hash))
                {
                    continue;
                }

                missing ??= new StringBuilder();
                missing.Append(missing.Length == 0 ? entry.Name : ", " + entry.Name);
            }

            if (missing == null)
            {
                return;
            }

            StringBuilder present = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    present.Append(", ");
                }

                present.Append(parameters[i].name);
            }

            UnityEngine.Debug.LogError(
                $"[AnimationOrchestrator] Animator on '{name}' is missing parameters: {missing}. "
                + $"Controller '{_animator.runtimeAnimatorController.name}' currently has "
                + $"[{present}]. Run SS3D → Animation → Rebind Humanoid Animator Parameters.",
                this);
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            // Coimbra UpdateEvent still fires after enabled=false — must guard or walk params keep writing.
            if (!isActiveAndEnabled || _posingSuppressed || _animator == null || !_animator.enabled)
            {
                return;
            }

            ApplyLocomotionVelocity();
            TickSwingTempo();
            TickUpperBodyWeight();
            TickAdditiveWeight();
        }

        /// <summary>
        /// Ragdoll/collapse sets this so Coimbra listeners cannot keep driving walk cycles.
        /// </summary>
        public void SetPosingSuppressed(bool suppressed)
        {
            _posingSuppressed = suppressed;
            if (suppressed && _animator != null)
            {
                _animator.SetFloat(Animations.Humanoid.MovementSpeed, 0f);
                _animator.SetFloat(Animations.Humanoid.VelX, 0f);
                _animator.SetFloat(Animations.Humanoid.VelZ, 0f);
                _animator.enabled = false;
            }
        }

        private void SubscribeToEvents()
        {
            if (_movementController != null)
            {
                _movementController.OnSpeedChangeEvent += HandleSpeedChanged;
                _movementController.OnLocomotionVelocityChanged += HandleLocomotionVelocityChanged;
            }
            if (_bodyStateMachine != null)
            {
                _bodyStateMachine.OnSnapshotChanged += ApplySnapshot;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_movementController != null)
            {
                _movementController.OnSpeedChangeEvent -= HandleSpeedChanged;
                _movementController.OnLocomotionVelocityChanged -= HandleLocomotionVelocityChanged;
            }
            if (_bodyStateMachine != null)
            {
                _bodyStateMachine.OnSnapshotChanged -= ApplySnapshot;
            }
        }

        private void HandleSpeedChanged(float speed)
        {
            // Controllers that publish VelX/VelZ (living, predicted) also fire the scalar gait
            // first each frame; it must not overwrite their strafe axes. A zero still lands —
            // HumanoidController sends only Speed 0 while the body cannot move.
            if (_controllerPublishesVelocity && speed > 0.001f)
            {
                return;
            }

            _targetSpeed = speed;
            if (_controllerPublishesVelocity)
            {
                _targetVelX = 0f;
                _targetVelZ = 0f;
            }

            _bodyStateMachine?.SetLocomotionSpeed(speed);
        }

        private void HandleLocomotionVelocityChanged(float velX, float velZ, float turn)
        {
            _controllerPublishesVelocity = true;
            _targetVelX = velX;
            _targetVelZ = velZ;
            _targetTurn = turn;
            _targetSpeed = new Vector2(velX, velZ).magnitude;
            _bodyStateMachine?.SetLocomotionVelocity(velX, velZ);
        }

        /// <summary>
        /// Plays a one-shot locomotion pack trigger (Jump / Turn90). Local visual for now.
        /// </summary>
        public void PlayLocomotionTrigger(int triggerHash)
        {
            if (_animator != null && triggerHash != 0)
            {
                _animator.SetTrigger(triggerHash);
            }
        }

        /// <summary>
        /// Owner-side immediate attack playback. Returns the swing variant used (0–2) for AttackSwing.
        /// </summary>
        public byte PlayAttackTrigger(AnimationTriggerId trigger, byte attackWindup = 0)
        {
            if (_animator == null)
            {
                return 0;
            }

            int hash = Animations.Humanoid.GetTriggerHash(trigger);
            if (hash == 0)
            {
                return 0;
            }

            byte variant = 0;
            if (trigger == AnimationTriggerId.AttackSwing)
            {
                variant = (byte)(_nextAttackVariant % MeleeSwingVariantCount);
                _nextAttackVariant = (_nextAttackVariant + 1) % MeleeSwingVariantCount;
                _animator.SetInteger(Animations.Humanoid.AttackVariant, variant);
                BeginSwingTempo(variant, attackWindup);
            }

            _ownerPredictedAttack = true;

            _animator.ResetTrigger(hash);
            _animator.SetTrigger(hash);
            return variant;
        }

        /// <summary>
        /// Plays the swing's wind-up at the rate that lands its strike frame at the weapon's effective
        /// windup (when the server connects), then lets the follow-through run at natural speed.
        /// </summary>
        private void BeginSwingTempo(int variant, byte attackWindup)
        {
            if (!_hasAttackRateParameter)
            {
                return;
            }

            float windupSeconds = MeleeSwingAnimation.DequantizeWindup(attackWindup);
            float rate = MeleeSwingAnimation.ComputeWindupRate(variant, windupSeconds);
            _animator.SetFloat(Animations.Humanoid.AttackRate, rate);
            _swingStrikeTime = MeleeSwingAnimation.TryGetSwing(variant, out MeleeSwingAnimation.Swing swing)
                ? Time.time + swing.StrikeSeconds / rate
                : float.NegativeInfinity;
        }

        private void TickSwingTempo()
        {
            if (!_hasAttackRateParameter || float.IsNegativeInfinity(_swingStrikeTime) || Time.time < _swingStrikeTime)
            {
                return;
            }

            _swingStrikeTime = float.NegativeInfinity;
            _animator.SetFloat(Animations.Humanoid.AttackRate, 1f);
        }

        private void TickUpperBodyWeight()
        {
            if (_animator == null || _animator.layerCount <= 1)
            {
                return;
            }

            _upperBodyWeight = Mathf.MoveTowards(
                _upperBodyWeight,
                _upperBodyWeightTarget,
                Time.deltaTime * _upperBodyWeightLerp);
            _animator.SetLayerWeight(1, _upperBodyWeight);
        }

        private void TickAdditiveWeight()
        {
            if (_animator == null || _animator.layerCount <= 2)
            {
                return;
            }

            _additiveWeight = Mathf.MoveTowards(
                _additiveWeight,
                _additiveWeightTarget,
                Time.deltaTime * _additiveWeightLerp);
            _animator.SetLayerWeight(2, _additiveWeight);
        }

        public void ApplySnapshot(BodyAnimationSnapshot snapshot)
        {
            EnsureAnimator();
            if (_posingSuppressed || _animator == null || !_animator.enabled)
            {
                return;
            }

            if (snapshot.State == BodyState.Ragdoll)
            {
                return;
            }

            _lastSnapshot = snapshot;
            if (!IsLocalMovementAuthority())
            {
                _targetSpeed = snapshot.MovementSpeed;
                _targetVelX = snapshot.MovementVelX;
                _targetVelZ = snapshot.MovementVelZ;
                _targetTurn = 0f;
            }
            ApplyLocomotion(snapshot);
            ApplyUpperBody(snapshot);
            ApplyCombat(snapshot);
            ApplyInjuries(snapshot);
            ApplyFullBodyOverride(snapshot);

            if (_bodyStateMachine != null)
            {
                byte sequence = _bodyStateMachine.TriggerSequence;
                if (sequence != _lastTriggerSequence && snapshot.ActiveTrigger != AnimationTriggerId.None)
                {
                    _lastTriggerSequence = sequence;
                    // Owner already played predictively — skip duplicate SetTrigger on host/client echo.
                    if (_ownerPredictedAttack)
                    {
                        _ownerPredictedAttack = false;
                        _lastConsumedTrigger = snapshot.ActiveTrigger;
                        OnTriggerFired?.Invoke(snapshot.ActiveTrigger);
                    }
                    else
                    {
                        ConsumeTrigger(snapshot.ActiveTrigger, snapshot.AttackVariant, snapshot.AttackWindup);
                    }
                }
            }
        }

        private void ApplyLocomotionVelocity()
        {
            if (IsLocalMovementAuthority())
            {
                // Controller publishes eased VelX/VelZ in step with world speed — no second lag here.
                _currentVelX = _targetVelX;
                _currentVelZ = _targetVelZ;
                _currentSpeed = _targetSpeed;
            }
            else
            {
                float lerp = Mathf.Clamp01(Time.deltaTime * _remoteLocomotionLerp);
                _currentVelX = Mathf.Lerp(_currentVelX, _targetVelX, lerp);
                _currentVelZ = Mathf.Lerp(_currentVelZ, _targetVelZ, lerp);
                _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, lerp);
            }

            _currentTurn = Mathf.Lerp(_currentTurn, _targetTurn, Mathf.Clamp01(Time.deltaTime * _turnLerp));

            _animator.SetFloat(Animations.Humanoid.VelX, _currentVelX);
            _animator.SetFloat(Animations.Humanoid.VelZ, _currentVelZ);
            _animator.SetFloat(Animations.Humanoid.Turn, _currentTurn);
            _animator.SetFloat(Animations.Humanoid.MovementSpeed, _currentSpeed);
            if (_hasLocoRateParameter)
            {
                _animator.SetFloat(
                    Animations.Humanoid.LocoRate,
                    _movementController != null ? _movementController.LocomotionPlaybackRate : 1f);
            }
        }

        private void ApplyLocomotion(BodyAnimationSnapshot snapshot)
        {
            _animator.SetBool(Animations.Humanoid.Floating, snapshot.IsFloating);
            _animator.SetBool(Animations.Humanoid.IsCrawling, snapshot.IsCrawling);
            _animator.SetInteger(Animations.Humanoid.LimpSide, (int)snapshot.LimpSide);
            _animator.SetBool(Animations.Humanoid.IsDragging, snapshot.IsDragging);
        }

        private void ApplyUpperBody(BodyAnimationSnapshot snapshot)
        {
            _animator.SetInteger(Animations.Humanoid.ArmHold, (int)snapshot.ArmHold);
            _animator.SetBool(Animations.Humanoid.IsSeated, snapshot.IsSeated);
            _animator.SetBool(Animations.Humanoid.MirrorUpperBody, snapshot.MirrorUpperBody);

            if (_animator.layerCount <= 1)
            {
                return;
            }

            // Peaceful: full base locomotion.
            // Melee: Upper Body weight 1 (Hold Default / AttackSwing).
            // Ranged: Upper Body weight 1 (Rifle Aim Idle / FireRifle / Reload) so shots don't
            // pulse layer weight; Base Ranged FreeformCartesian keeps the legs.
            bool needsUpperBodyLayer = snapshot.State != BodyState.Ragdoll
                && snapshot.CombatMode.IsCombat();
            _upperBodyWeightTarget = needsUpperBodyLayer ? 1f : 0f;
        }

        private void ApplyCombat(BodyAnimationSnapshot snapshot)
        {
            bool inCombat = snapshot.CombatMode.IsCombat();
            _animator.SetBool(Animations.Humanoid.CombatMode, inCombat);
            _animator.SetInteger(Animations.Humanoid.CombatStance, (int)snapshot.CombatMode);
            _animator.SetFloat(Animations.Humanoid.AimYaw, snapshot.AimYaw);
            _animator.SetFloat(Animations.Humanoid.AimPitch, snapshot.AimPitch);
            _ikController?.SetCombatLookAt(inCombat, snapshot.AimYaw, snapshot.AimPitch);
            _ikController?.SetRangedAimActive(snapshot.CombatMode == HumanoidCombatMode.Ranged && snapshot.State != BodyState.Ragdoll);

            // Hold Item/Weapon have no path to Rifle Aim Idle unless wired — CrossFade on stance
            // enter so a held gun doesn't show melee Hold Weapon until the first shot.
            if (snapshot.CombatMode == HumanoidCombatMode.Ranged
                && _lastAppliedCombatMode != HumanoidCombatMode.Ranged)
            {
                EnsureUpperBodyState(RifleAimIdleStateHash, 0.1f);
            }
            else if (snapshot.CombatMode == HumanoidCombatMode.Melee
                     && _lastAppliedCombatMode == HumanoidCombatMode.Ranged)
            {
                EnsureUpperBodyState(HoldDefaultStateHash, 0.1f);
            }

            _lastAppliedCombatMode = snapshot.CombatMode;
        }

        private void EnsureUpperBodyState(int stateHash, float fixedTransitionDuration)
        {
            if (_animator == null || _animator.layerCount <= 1)
            {
                return;
            }

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(1);
            if (current.shortNameHash == stateHash
                || current.shortNameHash == FireRifleStateHash
                || current.shortNameHash == ReloadRifleStateHash)
            {
                return;
            }

            if (_animator.IsInTransition(1))
            {
                int next = _animator.GetNextAnimatorStateInfo(1).shortNameHash;
                if (next == stateHash || next == FireRifleStateHash || next == ReloadRifleStateHash)
                {
                    return;
                }
            }

            _animator.CrossFadeInFixedTime(stateHash, fixedTransitionDuration, 1);
        }

        private void ApplyInjuries(BodyAnimationSnapshot snapshot)
        {
            _animator.SetFloat(Animations.Humanoid.InjuredArmLeft, snapshot.InjuredArmLeft);
            _animator.SetFloat(Animations.Humanoid.InjuredArmRight, snapshot.InjuredArmRight);
            _animator.SetFloat(Animations.Humanoid.InjuredLeg, snapshot.InjuredLeg);

            if (_animator.layerCount > 2)
            {
                float armMax = Mathf.Max(snapshot.InjuredArmLeft, snapshot.InjuredArmRight);
                float leg = snapshot.InjuredLeg;

                float injuryWeight;
                if (leg >= StumbleLegThreshold && leg >= armMax)
                {
                    // Stumble idle already reads on base; keep arm additive light.
                    injuryWeight = armMax * 0.25f;
                }
                else if (armMax > 0.01f)
                {
                    injuryWeight = Mathf.Lerp(0.15f, 0.55f, armMax);
                }
                else
                {
                    injuryWeight = 0f;
                }

                // Soft target only — TickAdditiveWeight lerps. Stagger raises Additive so unmuted
                // Additive Flinch (gut) shows; never slam Empty Additive (hurting idle) to 1.
                _additiveWeightTarget = snapshot.State == BodyState.Staggered
                    ? Mathf.Max(injuryWeight, StaggerAdditiveWeight)
                    : injuryWeight;
            }
        }

        private void ApplyFullBodyOverride(BodyAnimationSnapshot snapshot)
        {
            if (_animator.layerCount <= 3)
            {
                return;
            }

            // Staggered uses Base + Additive Flinch — do not half-weight Full Body Override.
            float overrideWeight = snapshot.State switch
            {
                BodyState.Seated => 1f,
                BodyState.Crawling => 1f,
                _ => 0f,
            };
            _animator.SetLayerWeight(3, overrideWeight);
        }

        private void ConsumeTrigger(AnimationTriggerId trigger, byte attackVariant = 0, byte attackWindup = 0)
        {
            // Sequence already gates re-entry; allow the same trigger id to fire repeatedly (e.g. swing spam).
            if (trigger == AnimationTriggerId.None)
            {
                return;
            }

            _lastConsumedTrigger = trigger;
            if (trigger == AnimationTriggerId.AttackSwing)
            {
                _animator.SetInteger(Animations.Humanoid.AttackVariant, attackVariant & 0x3);
                BeginSwingTempo(attackVariant & 0x3, attackWindup);
            }

            int hash = Animations.Humanoid.GetTriggerHash(trigger);
            if (hash != 0)
            {
                _animator.ResetTrigger(hash);
                _animator.SetTrigger(hash);
            }

            OnTriggerFired?.Invoke(trigger);
        }

        public void FireLocalTrigger(AnimationTriggerId trigger)
        {
            _bodyStateMachine?.CmdFireTrigger(trigger, 0, 0);
        }

        private bool IsLocalMovementAuthority()
        {
            if (_movementController is NetworkBehaviour networkBehaviour)
            {
                return networkBehaviour.IsOwner;
            }

            return false;
        }
    }
}
