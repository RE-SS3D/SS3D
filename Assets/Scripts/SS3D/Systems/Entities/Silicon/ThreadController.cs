using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Entities.Silicon
{
    /// <summary>
    /// Controls the movement for threaded characters
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class ThreadController : NetworkedSpessBehaviour
    {
        public event Action<float> OnSpeedChanged;
        public event Action<bool> OnPowerChanged; 

        [Header("Components")] 
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerControllable _playerControllable;
        
        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed;
        [SerializeField] private float _lerpMultiplier;
        [SerializeField] private float _rotationLerpMultiplier;
        
        [SerializeField] private Transform _movementTarget;
        
        [Header("Debug Info")] 
        private Vector3 _absoluteMovement;
        private Vector2 _input;
        private Vector2 _smoothedInput;
        
        private Vector3 _targetMovement;
        
        private float _smoothedX;
        private float _smoothedY;
        private SpessBehaviour _camera;
        
        protected override void OnAwake()
        {
            base.OnAwake();
        
            Setup();
        }

        private void Setup()
        {
            _camera = GameSystems.Get<CameraSystem>().PlayerCamera;

            _playerControllable.ControllingSoulChanged += HandleControllingSoulChanged;
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
        private void ProcessCharacterMovement()
        {
            ProcessPlayerInput();
            _characterController.Move(Physics.gravity);
            
            if (_input.magnitude != 0)
            {
                MoveMovementTarget(_input);
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
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
        private void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
        {
            //makes the movement align to the camera view
            Vector3 newTargetMovement =
                movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized +
                movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized;
            
            // smoothly changes the target movement
            _targetMovement = Vector3.Lerp(_targetMovement, newTargetMovement, Time.deltaTime * (_lerpMultiplier * multiplier));
            
            Vector3 resultingMovement = _targetMovement + transform.position;
            _absoluteMovement = resultingMovement;
        
            _movementTarget.position = _absoluteMovement;
        }
        
        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        private void RotatePlayerToMovement()
        {
            transform.rotation = 
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_targetMovement), Time.deltaTime * _rotationLerpMultiplier);
        }
        
        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        private void MovePlayer()
        {
            _characterController.Move(_targetMovement * ((_movementSpeed) * Time.deltaTime));
        }
        
        /// <summary>
        /// Process the player movement input, smoothing it 
        /// </summary>
        /// <returns></returns>
        private void ProcessPlayerInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
        
            _input = new Vector2(x, y);
            OnSpeedChanged?.Invoke(_input.magnitude != 0 ? _input.magnitude : 0);
        
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, Time.deltaTime * (_lerpMultiplier / 10));
        }

        private void HandleControllingSoulChanged(Soul soul)
        {
            OnPowerChanged?.Invoke(soul != null);
        }
    }
}
