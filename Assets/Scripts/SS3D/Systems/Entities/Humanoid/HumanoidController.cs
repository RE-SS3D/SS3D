﻿using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(Animator))]
    public abstract class HumanoidController : NetworkActor
    {
        #region Fields
        public event Action<float> SpeedChangeEvent;

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
        protected Vector3 AbsoluteMovement;
        protected Vector2 Input;
        protected Vector2 SmoothedInput;
        protected Vector3 TargetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;
        private InputSystem _inputSystem;
        private const float _walkAnimatorValue = .3f;
        private const float _runAnimatorValue = 1f;
        #endregion

        #region Properties
        public virtual float WalkAnimatorValue => _walkAnimatorValue;
        public virtual float RunAnimatorValue => _runAnimatorValue;
        public bool IsRunning => _isRunning;
        #endregion

        protected override void OnStart()
        {
            base.OnStart();
            if (!Owner.IsLocalClient) return;
            Setup();
        }

        protected void Setup()
        {
            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _entity.OnMindChanged += HandleControllingPlayerChanged;
            _inputSystem = Subsystems.Get<InputSystem>();
            Controls controls = _inputSystem.Inputs;
            MovementControls = controls.Movement;
            HotkeysControls = controls.Hotkeys;
            MovementControls.ToggleRun.performed += HandleToggleRun;
            _inputSystem.ToggleActionMap(MovementControls, true);
            _inputSystem.ToggleActionMap(HotkeysControls, true);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
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
        /// Gets the mouse position and updates the mouse IK targets while maintaining the player height
        /// </summary>
        private void UpdateMousePositionTransforms()
        {
            // Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            // Vector3 mousePos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            // mousePos = new Vector3(mousePos.x, transform.position.y, mousePos.z);
            
            // _mouseDirectionTransform.LookAt(mousePos);
            // _mousePositionTransform.position = mousePos;
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
            SpeedChangeEvent?.Invoke(speed);
        }
    }

}