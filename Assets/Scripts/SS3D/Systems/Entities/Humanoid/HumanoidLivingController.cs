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
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidLivingController : HumanoidController
    {

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private StaminaController _staminaController;
        private HumanHealthController _healthController;

        public bool IsDragging { get; set; }



		public override void OnStartClient()
        {
            base.OnStartClient();
            _healthController = GetComponent<HumanHealthController>();
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
            if (_healthController != null && !_healthController.Snapshot.IsConscious)
            {
                _characterController.Move(Physics.gravity);
                MoveMovementTarget(Vector2.zero, 5);
                MovePlayer();
                return;
            }

            ProcessPlayerInput();

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
            return IsRunning && _staminaController.CanContinueInteraction ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            float healthMultiplier = _healthController != null
                ? _healthController.Snapshot.MovementSpeedMultiplier
                : 1f;
            _characterController.Move(TargetMovement * (_movementSpeed * healthMultiplier * Time.deltaTime));
        }
    }

}