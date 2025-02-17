using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Camera;
using SS3D.Systems.Health;
using SS3D.Systems.Inputs;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidMovementController : NetworkActor, ISpeedProvider
    {
        public event Action<float> OnSpeedChangeEvent;

        private const float DefaultSpeed = 1f;
        private const float RunFactor = 3f;
        private const float DragFactor = 0.5f;
        private const float CrouchFactor = 0.6f;
        private const float AimFactor = 0.6f;
        private const float CrawlFactor = 0.4f;

        [SerializeField]
        private Rigidbody _rb;

        [Header("Movement Settings")]
        [SerializeField]
        private float _movementSpeed;

        [SerializeField]
        private float _lerpMultiplier;

        [SerializeField]
        private float _rotationLerpMultiplier;

        [Header("Movement IK Targets")]
        [SerializeField]
        private Transform _movementTarget;

        [Header("Debug Info")]
        private Vector3 _absoluteMovement;
        private Vector2 _input;
        private Vector2 _smoothedInput;

        private CameraActor _camera;
        private Controls.MovementActions _movementControls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputSystem _inputSystem;

        [SerializeField]
        private float _aimRotationSpeed = 5f;

        [SerializeField]
        private PositionController _positionController;

        public float InputAimAngle { get; private set; }

        [field: SerializeField]
        public Vector3 TargetMovement { get; private set; }

        [field: SerializeField]
        public Transform AimTarget { get; private set; }

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Return the max speed taking in count the movement type and the position
        /// </summary>
        /// <returns></returns>
        public float MaxSpeed => ComputeSpeed() / DefaultSpeed * (IsRunning ? DefaultSpeed : RunFactor);

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;

                return;
            }

            Setup();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            _movementControls.ToggleRun.performed -= HandleToggleRun;
            InstanceFinder.TimeManager.OnTick -= HandleNetworkTick;
            _inputSystem.ToggleActionMap(_movementControls, false);
            _inputSystem.ToggleActionMap(_hotkeysControls, false);
        }

        [Client]
        private void HandleNetworkTick()
        {
            if (!enabled)
            {
                return;
            }

            ProcessCharacterMovement();

            if (_positionController.Movement == MovementType.Aiming)
            {
                ComputeAngleBetweenAimAndInput();
            }
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets.
        /// </summary>
        [Client]
        private void ProcessCharacterMovement()
        {
            ProcessPlayerInput();
            MoveMovementTarget(_input, _input.magnitude == 0 ? 5 : 1);

            _rb.velocity = TargetMovement * (float)(_movementSpeed * TimeManager.TickDelta);

            if (_positionController.Movement != MovementType.Aiming && _input.magnitude != 0)
            {
                RotatePlayerToMovement(_positionController.Movement == MovementType.Dragging);
            }

            if (_positionController.CanRotateWhileAiming)
            {
                RotatePlayerTowardTarget();
            }
        }

        /// <summary>
        /// Moves the movement targets with the given input.
        /// </summary>
        /// <param name="movementInput"></param>
        [Client]
        private void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
        {
            Vector3 newTargetMovement = (movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized) + (movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized);

            // smoothly changes the target movement
            TargetMovement = Vector3.Lerp(TargetMovement, newTargetMovement, (float)(TimeManager.TickDelta * (_lerpMultiplier * multiplier)));

            Vector3 resultingMovement = TargetMovement + Position;
            _absoluteMovement = resultingMovement;
            _movementTarget.position = _absoluteMovement;
        }

        /// <summary>
        /// Rotates the player to the target movement.
        /// </summary>
        [Client]
        private void RotatePlayerToMovement(bool lookOpposite)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookOpposite ? -TargetMovement : TargetMovement);
            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, (float)(TimeManager.TickDelta * _rotationLerpMultiplier));
        }

        /// <summary>
        /// Process the player movement input, smoothing it.
        /// </summary>
        [Client]
        private void ProcessPlayerInput()
        {
            float x = _movementControls.Movement.ReadValue<Vector2>().x;
            float y = _movementControls.Movement.ReadValue<Vector2>().y;

            float inputFilteredSpeed = ComputeSpeed();

            _input = new Vector2(x, y).normalized * inputFilteredSpeed;
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, (float)(TimeManager.TickDelta * (_lerpMultiplier / 10)));

            OnSpeedChangeEvent?.Invoke(_input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        [Client]
        private float ComputeSpeed()
        {
            float speed = DefaultSpeed;
            speed *= IsRunning ? RunFactor : 1;
            speed *= _positionController.Movement == MovementType.Aiming ? AimFactor : 1;
            speed *= _positionController.Movement == MovementType.Dragging ? DragFactor : 1;
            speed *= GetComponent<PositionController>().PositionType == PositionType.Proning ? CrawlFactor : 1;
            speed *= GetComponent<PositionController>().PositionType == PositionType.Crouching ? CrouchFactor : 1;

            return speed;
        }

        /// <summary>
        /// Toggles your movement between run/walk.
        /// </summary>
        [Client]
        private void HandleToggleRun(InputAction.CallbackContext context)
        {
            IsRunning = !IsRunning;
        }

        [Client]
        private void Setup()
        {
            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _inputSystem = Subsystems.Get<InputSystem>();

            Controls controls = _inputSystem.Inputs;

            _movementControls = controls.Movement;
            _hotkeysControls = controls.Hotkeys;
            _movementControls.ToggleRun.performed += HandleToggleRun;

            _inputSystem.ToggleActionMap(_movementControls, true);
            _inputSystem.ToggleActionMap(_hotkeysControls, true);
            InstanceFinder.TimeManager.OnTick += HandleNetworkTick;
            _positionController.OnChangedPosition += HandleChangedPosition;
        }

        private void HandleChangedPosition(PositionType position, float durationTransition)
        {
            enabled = position != PositionType.RagdollRecover && position != PositionType.Ragdoll && position != PositionType.ResetBones;
        }

        [Client]
        private void ComputeAngleBetweenAimAndInput()
        {
            // Convert the target's position to 2D (XZ plane)
            Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
            Vector2 targetMove = new Vector2(TargetMovement.x, TargetMovement.z);

            InputAimAngle = Vector2.SignedAngle(targetMove, forward);
        }

        [Client]
        private void RotatePlayerTowardTarget()
        {
            // Get the direction to the target
            Vector3 direction = AimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, (float)(_aimRotationSpeed * TimeManager.TickDelta));
            }
        }
    }
}
