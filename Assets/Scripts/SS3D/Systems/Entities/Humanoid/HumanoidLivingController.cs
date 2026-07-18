using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Stamina;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for living biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(AnimationOrchestrator))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidLivingController : HumanoidController
    {

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private StaminaController _staminaController;
        private HumanHealthController _healthController;
        [SerializeField] private HumanoidPredictedMovement _predictedMovement;

        public bool IsDragging { get; set; }



		public override void OnStartClient()
        {
            base.OnStartClient();
            _healthController = GetComponent<HumanHealthController>();
            if (_predictedMovement == null)
            {
                _predictedMovement = GetComponent<HumanoidPredictedMovement>();
            }
            if (!IsOwner)
            {
                return;
            }    
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected override void ProcessCharacterMovement()
        {
            if (_healthController != null
                && (!_healthController.Snapshot.IsConscious || _healthController.Snapshot.IsCardiacArrest))
            {
                _characterController.Move(Physics.gravity);
                MoveMovementTarget(Vector2.zero, 5);
                MovePlayer();
                return;
            }

            ProcessPlayerInput();

            if (_predictedMovement != null && _predictedMovement.enabled)
            {
                return;
            }

            _characterController.Move(Physics.gravity);

            float gaitSpeed = FilterSpeed();
            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                if (!IsDragging)
                {
                    if (IsCombatMode())
                    {
                        RotatePlayerToCombatAim();
                    }
                    else
                    {
                        RotatePlayerToMovement();
                    }
                }

                MovePlayer();
                PublishLocomotionVelocity(TargetMovement, gaitSpeed);
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
                if (IsCombatMode() && !IsDragging)
                {
                    RotatePlayerToCombatAim();
                }

                PublishLocomotionVelocity(Vector3.zero, 0f);
            }
        }

        protected override float FilterSpeed()
        {
            // Exhaustion no longer hard-blocks run (stamina.md §3); ExertionPenalty slows MovePlayer.
            return IsRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            // Health rewrite: MovementSpeedMultiplier subsumes the legacy per-foot FeetHealthFactor
            // (it already derives from LeftLeg/RightLeg zone damage). Combat scaling is orthogonal.
            float healthMultiplier = _healthController != null
                ? _healthController.Snapshot.MovementSpeedMultiplier
                : 1f;
            float combatFactor = 1f;
            // Combat walk/run clips are authored at the same cadence across stances (melee + ranged),
            // so apply slow combat speed scaling for any combat mode.
            if (IsCombatMode())
            {
                combatFactor = IsRunning ? _combatRunSpeedFactor : _combatWalkSpeedFactor;
            }

            float exertionFactor = _staminaController != null
                ? Mathf.Lerp(1f, 0.55f, _staminaController.ExertionPenalty)
                : 1f;

            _characterController.Move(
                TargetMovement * (_movementSpeed * healthMultiplier * combatFactor * exertionFactor * Time.deltaTime));
        }
    }

}