using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
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
		[SerializeField] private FeetController _feetController;
        [SerializeField] private HumanoidPredictedMovement _predictedMovement;

        public bool IsDragging { get; set; }



		public override void OnStartClient()
        {
            base.OnStartClient();
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
            ProcessPlayerInput();

            if (_predictedMovement != null && _predictedMovement.enabled)
            {
                return;
            }

            _characterController.Move(Physics.gravity);

            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                if(!IsDragging) RotatePlayerToMovement();
                MovePlayer();
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
            }
        }

        protected override float FilterSpeed()
        {
            bool canRun = _staminaController == null || _staminaController.CanContinueInteraction;
            return IsRunning && canRun ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            float feetFactor = _feetController != null ? _feetController.FeetHealthFactor : 1f;
            _characterController.Move(TargetMovement * ((feetFactor * _movementSpeed) * Time.deltaTime));
        }
    }

}