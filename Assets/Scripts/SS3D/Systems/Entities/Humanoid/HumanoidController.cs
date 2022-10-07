using System;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(HumanoidAnimatorController))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidController : PlayerControllable
    {
        public event Action<float> OnAnimationSpeedChanged;

        [Header("Components")] 
        [SerializeField] private CharacterController _characterController;

        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed;
        [SerializeField] private float _lerpMultiplier;
        [SerializeField] private float _rotationLerpMultiplier;

        // [Header("Movement IK Targets")] 
        // [SerializeField] private Transform _movementTarget;
        // [SerializeField] private Transform _mousePositionTransform;
        // [SerializeField] private Transform _mouseDirectionTransform;

        [Header("Run/Walk")]
        private bool _isWalking;

        [Header("Debug Info")] 
        //private Vector3 _absoluteMovement;
        private Vector2 _input;
        private Vector2 _smoothedInput;
        private Vector3 _targetMovement;
        private float _smoothedX;
        private float _smoothedY;
        private Camera _camera;

        private const float WalkAnimatorValue = .3f;
        private const float RunAnimatorValue = 1f;

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            //camera = CameraManager.singleton.playerCamera;
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (ControlledByLocalPlayer)
            {
                ProcessCharacterMovement();
            }
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        private void ProcessCharacterMovement()
        {
            ProcessPlayerInput();
            ProcessToggleRun();

            _characterController.Move(Physics.gravity);
            
            if (_input.magnitude != 0)
            {
                // MoveMovementTarget(_input);
                RotatePlayerToMovement();
                MovePlayer();
            }
            else
            {
               // MoveMovementTarget(Vector2.zero);   
            }
                
            UpdateMousePositionTransforms();
        }
    
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
        // private void MoveMovementTarget(Vector2 movementInput)
        // {
            // makes the movement align to the camera view
            // Vector3 newTargetMovement =
            //     movementInput.y * Vector3.Cross(_camera.transform.right, Vector3.up).normalized +
            //     movementInput.x * Vector3.Cross(Vector3.up, _camera.transform.forward).normalized;
            //
            // // smoothly changes the target movement
            // _targetMovement = Vector3.Lerp(_targetMovement, newTargetMovement, Time.deltaTime * _lerpMultiplier);
            //
            // Vector3 resultingMovement = _targetMovement + transform.position;
            //_absoluteMovement = resultingMovement;

            // _movementTarget.position = _absoluteMovement;
        // }

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
            
            float inputFilteredSpeed = _isWalking ? WalkAnimatorValue : RunAnimatorValue;
            
            x = Mathf.Clamp(x, -inputFilteredSpeed, inputFilteredSpeed);
            y = Mathf.Clamp(y, -inputFilteredSpeed, inputFilteredSpeed);
            
            _input = new Vector2(x, y);
            OnAnimationSpeedChanged?.Invoke(_input.magnitude != 0 ? inputFilteredSpeed : 0);

            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, Time.deltaTime * (_lerpMultiplier / 10));
        }
        
        /// <summary>
        /// Toggles your movement between run/walk
        /// </summary>
        private void ProcessToggleRun()
        {
            if (Input.GetButtonDown("Toggle Run"))
            {
                _isWalking = !_isWalking;
            }
        }
    }
    
}