using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
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
        protected Vector3 _absoluteMovement;
        protected Vector2 _input;
        protected Vector2 _smoothedInput;
        protected Vector3 _targetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;
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
            
            Setup();
        }

        protected void Setup()
        {
            _camera = SystemLocator.Get<CameraSystem>().PlayerCamera;
            _entity.OnMindChanged += HandleControllingSoulChanged;
            Controls controls = SystemLocator.Get<InputSystem>().Inputs;
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

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

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
            // Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
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
             _targetMovement = Vector3.Lerp(_targetMovement, newTargetMovement, Time.deltaTime * (_lerpMultiplier * multiplier));

             Vector3 resultingMovement = _targetMovement + Position;
            _absoluteMovement = resultingMovement;

             _movementTarget.position = _absoluteMovement;
         }

        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        protected void RotatePlayerToMovement()
        {
            Quaternion lookRotation = Quaternion.LookRotation(_targetMovement);

            transform.rotation =
                Quaternion.Slerp(Rotation, lookRotation, Time.deltaTime * _rotationLerpMultiplier);
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

            _input = new Vector2(x, y);
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, Time.deltaTime * (_lerpMultiplier / 10));

            OnSpeedChanged(_input.magnitude != 0 ? inputFilteredSpeed : 0);
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