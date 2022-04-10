using System;
using Mirror;
using SS3D.Content.Systems.Player.Movement;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Content.Systems.Player.Movement
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(BipedAnimatorController))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class BipedMovementController : NetworkBehaviour
    {
        public event Action<float> AnimationSpeedChanged;

        [Header("Components")] 
        [SerializeField] private CharacterController characterController;

        [Header("Movement Input Params")] 
        [SerializeField] private float globalSpeed;
        [SerializeField] private float movementSpeed;
        [SerializeField] private Vector2 input;
        [SerializeField] private Vector2 smoothedInput;
        [SerializeField] private float lerpMultiplier;
        [SerializeField] private float rotationLerpMultiplier;
        [SerializeField] private AnimationCurve inputSmoothnessCurve;
        
        Vector3 targetMovement;
        private float _smoothedX;
        private float _smoothedY;
        
        [Header("Run/Walk")]
        [SerializeField] private bool isWalking;
        private const float walkAnimatorValue = .3f;
        private const float runAnimatorValue = 1f;

        [Header("Movement Targets")] 
        [SerializeField] private Transform movementTarget;
        [SerializeField] private Transform mousePositionTransform;
        [SerializeField] private Transform mouseDirectionTransform;

        [Header("Debug Info")] 
        [SerializeField] private Vector3 _absoluteMovement;

        private new Camera camera;

        public Vector3 AbsoluteMovement => _absoluteMovement; 

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            camera = CameraManager.singleton.playerCamera;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;
            
            ProcessCharacterMovement();
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        private void ProcessCharacterMovement()
        {
            Vector2 movement = ProcessPlayerInput();
            
            ProcessToggleRun();
            characterController.Move(Physics.gravity);
            
            if (input.magnitude != 0)
            {
                MoveMovementTarget(input);
                RotatePlayerToMovement();
                MovePlayer();
            }
            else
            {
                MoveMovementTarget(Vector2.zero);   
            }
                
            UpdateMousePositionTransforms();
        }
    
        /// <summary>
        /// Gets the mouse position and updates the mouse IK targets while maintaining the player height
        /// </summary>
        private void UpdateMousePositionTransforms()
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Vector3 mousePos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            mousePos = new Vector3(mousePos.x, transform.position.y, mousePos.z);
            
            mouseDirectionTransform.LookAt(mousePos);
            mousePositionTransform.position = mousePos;
        }
        
        /// <summary>
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
        private void MoveMovementTarget(Vector2 movementInput)
        {
            // makes the movement align to the camera view
            Vector3 newTargetMovement =
                movementInput.y * Vector3.Cross(camera.transform.right, Vector3.up).normalized +
                movementInput.x * Vector3.Cross(Vector3.up, camera.transform.forward).normalized;

            // smoothly changes the target movement
            targetMovement = Vector3.Lerp(targetMovement, newTargetMovement, Time.deltaTime * lerpMultiplier);

            Vector3 resultingMovement = targetMovement + transform.position;
            _absoluteMovement = resultingMovement;

            movementTarget.position = _absoluteMovement;
        }

        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        private void RotatePlayerToMovement()
        {
            transform.rotation = 
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetMovement), Time.deltaTime * rotationLerpMultiplier);
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        private void MovePlayer()
        {
            characterController.Move(targetMovement * ((movementSpeed) * Time.deltaTime));
        }
        
        /// <summary>
        /// Process the player movement input, smoothing it 
        /// </summary>
        /// <returns></returns>
        private Vector2 ProcessPlayerInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            
            float inputFilteredSpeed = isWalking ? walkAnimatorValue : runAnimatorValue;
            
            x = Mathf.Clamp(x, -inputFilteredSpeed, inputFilteredSpeed);
            y = Mathf.Clamp(y, -inputFilteredSpeed, inputFilteredSpeed);
            
            input = new Vector2(x, y);
            AnimationSpeedChanged?.Invoke(input.magnitude != 0 ? inputFilteredSpeed : 0);

            smoothedInput = Vector2.Lerp(smoothedInput, input, Time.deltaTime * (lerpMultiplier / 10));

            Vector2 resultedMovement = smoothedInput;

            return resultedMovement;
        }
        
        /// <summary>
        /// Toggles your movement between run/walk
        /// </summary>
        private void ProcessToggleRun()
        {
            if (Input.GetButtonDown("Toggle Run"))
                isWalking = !isWalking;
        }
    }
    
}