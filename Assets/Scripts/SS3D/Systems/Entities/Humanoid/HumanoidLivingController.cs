using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for living biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidLivingController : HumanoidController
    {

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private StaminaController _staminaController;

		List<FootBodyPart> feet;

		private int _optimalFeetNumber = 2;

		[SyncVar] private float _feetHealthFactor;

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (!IsOwner)
			{
				return;
			}	
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			feet = GetComponentsInChildren<FootBodyPart>().ToList();
			foreach (FootBodyPart foot in feet)
			{
				foot.OnDamageInflicted += HandleFootHurt;
				foot.OnBodyPartDetached += HandleFootRemoved;
				foot.OnBodyPartDestroyed += HandleFootRemoved;
			}
			_feetHealthFactor = 1;
		}

		/// <summary>
		/// Executes the movement code and updates the IK targets
		/// </summary>
		protected override void ProcessCharacterMovement()
        {
            ProcessPlayerInput();

            _characterController.Move(Physics.gravity);

            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                RotatePlayerToMovement();
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
            return IsRunning && _staminaController.CanContinueInteraction ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            _characterController.Move(TargetMovement * ((_feetHealthFactor * _movementSpeed) * Time.deltaTime));
        }

		private void HandleFootHurt(object sender, EventArgs e)
		{
			UpdateFeetHealthFactor();
		}

		private void HandleFootRemoved(object sender, EventArgs e)
		{
			FootBodyPart foot = (FootBodyPart)sender;
			if (foot != null)
			{
				feet.Remove(foot);
			}
			UpdateFeetHealthFactor();
		}

		private void UpdateFeetHealthFactor()
		{
			_feetHealthFactor = feet.Sum(x => x.GetSpeedContribution()) / _optimalFeetNumber;
		}
	}

}