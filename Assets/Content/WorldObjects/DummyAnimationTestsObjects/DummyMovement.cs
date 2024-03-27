using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;


public class DummyMovement : Actor
    {

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected void ProcessCharacterMovement()
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

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected void MovePlayer()
        {
            _characterController.Move(TargetMovement * ( _movementSpeed * Time.deltaTime));
        }
        
         public event Action<float> OnSpeedChangeEvent;
        

        [Header("Movement Settings")]
        [SerializeField] protected float _movementSpeed;
        [SerializeField] protected float _lerpMultiplier;
        [SerializeField] protected float _rotationLerpMultiplier;

        [Header("Movement IK Targets")]
        [SerializeField] private Transform _movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        protected Vector3 AbsoluteMovement;
        protected Vector2 Input;
        protected Vector2 SmoothedInput;
        public Vector3 TargetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;
        private InputSystem _inputSystem;
        private const float _walkAnimatorValue = .3f;
        private const float _runAnimatorValue = 1f;
        
        public virtual float WalkAnimatorValue => _walkAnimatorValue;
        public virtual float RunAnimatorValue => _runAnimatorValue;
        public bool IsRunning => _isRunning;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        protected void Setup()
        {
            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _inputSystem = Subsystems.Get<InputSystem>();

            Controls controls = _inputSystem.Inputs;

            MovementControls = controls.Movement;
            HotkeysControls = controls.Hotkeys;
            MovementControls.ToggleRun.performed += HandleToggleRun;

            _inputSystem.ToggleActionMap(MovementControls, true);
            _inputSystem.ToggleActionMap(HotkeysControls, true);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnDisabled()
        {
	        base.OnDisabled();
	        TargetMovement = Vector3.zero;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnityEngine.Debug.Log("destroying controller " + gameObject.name);
            MovementControls.ToggleRun.performed -= HandleToggleRun;
            _inputSystem.ToggleActionMap(MovementControls, false);
            _inputSystem.ToggleActionMap(HotkeysControls, false);
        }

        private void HandleControllingPlayerChanged(Mind mind)
        {
            OnSpeedChanged(0);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
	        if (!enabled)
	        {
		        return;
	        }
            ProcessCharacterMovement();
        }

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
            AbsoluteMovement = resultingMovement;
             _movementTarget.position = AbsoluteMovement;
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
        /// Process the player movement input, smoothing it
        /// </summary>
        /// <returns></returns>
        protected void ProcessPlayerInput()
        {
            float x = MovementControls.Movement.ReadValue<Vector2>().x;
            float y = MovementControls.Movement.ReadValue<Vector2>().y;

            float inputFilteredSpeed = FilterSpeed();

            Input = Vector2.ClampMagnitude(new Vector2(x, y), inputFilteredSpeed);
            SmoothedInput = Vector2.Lerp(SmoothedInput, Input, Time.deltaTime * (_lerpMultiplier / 10));

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

        protected void OnSpeedChanged(float speed)
        {
            OnSpeedChangeEvent?.Invoke(speed);
        }
}
