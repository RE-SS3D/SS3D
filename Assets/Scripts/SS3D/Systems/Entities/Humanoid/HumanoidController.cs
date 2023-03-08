using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(Animator))]
    public abstract class HumanoidController : NetworkActor
    {
        public event Action<float> OnSpeedChangeEvent;

        [Header("Components")] 
        [SerializeField] protected Entity _entity;

        [Header("Movement Settings")]
        [SerializeField] protected float _movementSpeed;
        [SerializeField] protected float _lerpMultiplier;
        [SerializeField] protected float _rotationLerpMultiplier;

        [Header("Movement IK Targets")]
        [SerializeField] private Transform _movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        private Vector3 _absoluteMovement;
        private Vector2 _smoothedInput;

        protected Vector2 Input;
        protected Vector3 TargetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;

        public const float WalkAnimatorValue = .3f;
        public const float RunAnimatorValue = 1f;

        public bool IsRunning => _isRunning;

        protected override void OnEnabled()
        {
            base.OnEnabled();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));

            Setup();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            _entity.OnMindChanged -= HandleControllingSoulChanged;
        }

        private void Setup()
        {
            _camera = Subsystems.Get<CameraSubsystem>().PlayerCamera;
            _entity.OnMindChanged += HandleControllingSoulChanged;
            Controls controls = Subsystems.Get<InputSubsystem>().Inputs;
            MovementControls = controls.Movement;
            HotkeysControls = controls.Hotkeys;
            MovementControls.ToggleRun.performed += HandleToggleRun;
            MovementControls.Enable();
            HotkeysControls.Enable();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            MovementControls.ToggleRun.performed -= HandleToggleRun;
        }

        private void HandleControllingSoulChanged(Mind mind)
        {
            OnSpeedChanged(0);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsOwner)
            {
                return;
            }

            ProcessCharacterMovement();
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected abstract void ProcessCharacterMovement();

        /// <summary>
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
         protected void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
         {
             //makes the movement align to the camera view
             Vector3 newTargetMovement =
                 movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized +
                 movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized;

             // smoothly changes the target movement
             TargetMovement = Vector3.Lerp(TargetMovement, newTargetMovement, Time.deltaTime * (_lerpMultiplier * multiplier));

             Vector3 resultingMovement = TargetMovement + Position;
            _absoluteMovement = resultingMovement;

             _movementTarget.position = _absoluteMovement;
         }

        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        protected void RotatePlayerToMovement()
        {
            Quaternion lookRotation = Quaternion.LookRotation(TargetMovement);

            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, Time.deltaTime * _rotationLerpMultiplier);
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected abstract void MovePlayer();
        
        /// <summary>
        /// Process the player movement input, smoothing it
        /// </summary>
        /// <returns></returns>
        protected void ProcessPlayerInput()
        {
            float x = MovementControls.Movement.ReadValue<Vector2>().x;
            float y = MovementControls.Movement.ReadValue<Vector2>().y;
            float inputFilteredSpeed = FilterSpeed();

            x = Mathf.Clamp(x, -inputFilteredSpeed, inputFilteredSpeed);
            y = Mathf.Clamp(y, -inputFilteredSpeed, inputFilteredSpeed);

            Input = new Vector2(x, y);
            _smoothedInput = Vector2.Lerp(_smoothedInput, Input, Time.deltaTime * (_lerpMultiplier / 10));

            OnSpeedChanged(Input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        protected virtual float FilterSpeed()
        {
            return _isRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Toggles your movement between run/walk
        /// </summary>
        protected void HandleToggleRun(InputAction.CallbackContext context)
        {
            _isRunning = !_isRunning;
        }

        private void OnSpeedChanged(float speed)
        {
            OnSpeedChangeEvent?.Invoke(speed);
        }
    }

}