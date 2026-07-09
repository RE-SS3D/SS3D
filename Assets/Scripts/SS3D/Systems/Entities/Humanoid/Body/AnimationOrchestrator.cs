using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;
using System;
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
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_movementController != null)
            {
                _movementController.OnSpeedChangeEvent += HandleSpeedChanged;
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
            }
            if (_bodyStateMachine != null)
            {
                _bodyStateMachine.OnSnapshotChanged -= ApplySnapshot;
            }
        }

        private void HandleSpeedChanged(float speed)
        {
            _bodyStateMachine?.SetLocomotionSpeed(speed);
            ApplyMovementSpeed(speed);
        }

        public void ApplySnapshot(BodyAnimationSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            ApplyMovementSpeed(snapshot.MovementSpeed);
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

        private void ApplyMovementSpeed(float speed)
        {
            bool isMoving = speed != 0f;
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3f);
            _currentSpeed = Mathf.Lerp(_currentSpeed, speed, Time.deltaTime * newLerpModifier);
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

            float upperBodyWeight = snapshot.State == BodyState.Ragdoll ? 0f : 1f;
            if (_animator.layerCount > 1)
            {
                _animator.SetLayerWeight(1, upperBodyWeight);
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
                float additiveWeight = Mathf.Max(snapshot.InjuredArmLeft, snapshot.InjuredArmRight) > 0.01f ? 1f : 0f;
                _animator.SetLayerWeight(2, additiveWeight);
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
    }
}
