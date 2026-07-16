using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;
using System;
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
        [SerializeField] private float _lerpMultiplier = 2.4f;
        [SerializeField] private HumanoidIkController _ikController;

        private float _currentSpeed;
        private float _targetSpeed;
        private float _currentVelX;
        private float _currentVelZ;
        private float _currentTurn;
        private float _targetVelX;
        private float _targetVelZ;
        private float _targetTurn;
        private BodyAnimationSnapshot _lastSnapshot = BodyAnimationSnapshot.Default;
        private AnimationTriggerId _lastConsumedTrigger = AnimationTriggerId.None;
        private byte _lastTriggerSequence;

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
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            SubscribeToEvents();

            if (_bodyStateMachine != null)
            {
                _targetSpeed = _bodyStateMachine.Snapshot.MovementSpeed;
                _targetVelZ = _targetSpeed;
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
            if (_animator == null)
            {
                return;
            }

            ApplyLocomotionVelocity();
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
            _targetSpeed = speed;
            _bodyStateMachine?.SetLocomotionSpeed(speed);
        }

        private void HandleLocomotionVelocityChanged(float velX, float velZ, float turn)
        {
            _targetVelX = velX;
            _targetVelZ = velZ;
            _targetTurn = turn;
            _targetSpeed = new Vector2(velX, velZ).magnitude;
            _bodyStateMachine?.SetLocomotionSpeed(_targetSpeed);
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

        public void ApplySnapshot(BodyAnimationSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            if (!IsLocalMovementAuthority())
            {
                _targetSpeed = snapshot.MovementSpeed;
                // Remotes do not yet replicate strafe axes — approximate with forward gait.
                _targetVelX = 0f;
                _targetVelZ = snapshot.MovementSpeed;
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
                    ConsumeTrigger(snapshot.ActiveTrigger);
                }
            }
        }

        private void ApplyLocomotionVelocity()
        {
            float currentMag = Mathf.Sqrt(_currentVelX * _currentVelX + _currentVelZ * _currentVelZ);
            float targetMag = Mathf.Sqrt(_targetVelX * _targetVelX + _targetVelZ * _targetVelZ);
            bool leavingIdle = currentMag < 0.05f && targetMag > currentMag;
            // Walk targets sit near 0.3; run is 1.0 — only snap the short idle→walk step.
            bool leavingIdleToWalk = leavingIdle && targetMag <= 0.45f;

            if (leavingIdleToWalk)
            {
                _currentVelX = _targetVelX;
                _currentVelZ = _targetVelZ;
                _currentTurn = _targetTurn;
                _currentSpeed = _targetSpeed;
            }
            else if (leavingIdle)
            {
                // Idle → run: begin at walk gait so acceleration passes through the blend tree.
                Vector2 target = new Vector2(_targetVelX, _targetVelZ);
                Vector2 walkSeed = target.normalized * 0.3f;
                _currentVelX = walkSeed.x;
                _currentVelZ = walkSeed.y;
                _currentTurn = _targetTurn;
                _currentSpeed = 0.3f;
            }
            else
            {
                bool slowingToIdle = targetMag < 0.05f;
                float lerp = Time.deltaTime * (slowingToIdle ? _lerpMultiplier * 3f : _lerpMultiplier);
                _currentVelX = Mathf.Lerp(_currentVelX, _targetVelX, lerp);
                _currentVelZ = Mathf.Lerp(_currentVelZ, _targetVelZ, lerp);
                _currentTurn = Mathf.Lerp(_currentTurn, _targetTurn, lerp);
                _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, lerp);
            }

            _animator.SetFloat(Animations.Humanoid.VelX, _currentVelX);
            _animator.SetFloat(Animations.Humanoid.VelZ, _currentVelZ);
            _animator.SetFloat(Animations.Humanoid.Turn, _currentTurn);
            _animator.SetFloat(Animations.Humanoid.MovementSpeed, _currentSpeed);
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

            if (_animator.layerCount > 1)
            {
                bool needsUpperBodyLayer = snapshot.State != BodyState.Ragdoll
                    && snapshot.ArmHold != ArmHoldPose.Default;
                _animator.SetLayerWeight(1, needsUpperBodyLayer ? 1f : 0f);
            }
        }

        private void ApplyCombat(BodyAnimationSnapshot snapshot)
        {
            _animator.SetBool(Animations.Humanoid.CombatMode, snapshot.CombatMode == HumanoidCombatMode.Combat);
            _animator.SetFloat(Animations.Humanoid.AimYaw, snapshot.AimYaw);
            _ikController?.SetCombatLookAt(snapshot.CombatMode == HumanoidCombatMode.Combat, snapshot.AimYaw);
        }

        private void ApplyInjuries(BodyAnimationSnapshot snapshot)
        {
            _animator.SetFloat(Animations.Humanoid.InjuredArmLeft, snapshot.InjuredArmLeft);
            _animator.SetFloat(Animations.Humanoid.InjuredArmRight, snapshot.InjuredArmRight);

            if (_animator.layerCount > 2)
            {
                bool injured = Mathf.Max(snapshot.InjuredArmLeft, snapshot.InjuredArmRight) > 0.01f;
                bool staggered = snapshot.State == BodyState.Staggered;
                _animator.SetLayerWeight(2, injured || staggered ? 1f : 0f);
            }
        }

        private void ApplyFullBodyOverride(BodyAnimationSnapshot snapshot)
        {
            if (_animator.layerCount <= 3)
            {
                return;
            }

            float overrideWeight = snapshot.State switch
            {
                BodyState.Seated => 1f,
                BodyState.Crawling => 1f,
                BodyState.Staggered => 0.5f,
                _ => 0f,
            };
            _animator.SetLayerWeight(3, overrideWeight);
        }

        private void ConsumeTrigger(AnimationTriggerId trigger)
        {
            if (trigger == AnimationTriggerId.None || trigger == _lastConsumedTrigger)
            {
                return;
            }

            _lastConsumedTrigger = trigger;
            int hash = Animations.Humanoid.GetTriggerHash(trigger);
            if (hash != 0)
            {
                _animator.SetTrigger(hash);
            }

            OnTriggerFired?.Invoke(trigger);
        }

        public void FireLocalTrigger(AnimationTriggerId trigger)
        {
            _bodyStateMachine?.CmdFireTrigger(trigger);
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
