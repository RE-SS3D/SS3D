using UnityEngine;
using Mirror;
using SS3D.Engine.Chat;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using UnityEngine.EventSystems;

namespace SS3D.Content.Systems.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidMovementController : NetworkBehaviour
    {
        public const float ACCELERATION = 25f;
        
        [Header("Movement Settings")]
        // The base speed at which the given character can move
        [SyncVar] public float runSpeed = 5f;
        // The base speed for the character when walking. To disable walkSpeed, set it to runSpeed
        [SyncVar] public float walkSpeed = 2f;
        [SerializeField] private AnimationCurve _movementCurve;
        [SerializeField] private float _movementCurveMultiplier;

        // Current movement the player is making.
        [Header("Movement Vectors")]
        private Vector2 _currentMovement = new Vector2();
        private Vector2 _intendedMovement = new Vector2();
        private Vector3 _absoluteMovement = new Vector3();

        public Vector3 AbsoluteMovement => _absoluteMovement;

        private bool isWalking = false;
        //Required to detect if player is typing and stop accepting movement input
        private ChatRegister chatRegister;

        [Header("Other")]
        [SerializeField] private float heightOffGround = 0.1f;
        [SerializeField] SimpleBodyPartLookAt[] LookAt;
        
        private Animator characterAnimator;
        private CharacterController characterController;
        private new Camera camera;

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            characterController = GetComponent<CharacterController>();
            characterAnimator = GetComponent<Animator>();
            chatRegister = GetComponent<ChatRegister>();
            camera = CameraManager.singleton.playerCamera; 
        }
        
        private void FixedUpdate()
        {
            //Must be the local player, or they cannot move
            if (!isLocalPlayer)
                return;

            ProcessToggleRun();
            ProcessCharacterMovementRefactor();
            ForceHeightLevel();
        }

        private void ProcessToggleRun()
        {
            if (Input.GetButtonDown("Toggle Run") && EventSystem.current.currentSelectedGameObject == null)
                isWalking = !isWalking;
        }

        private void ProcessCharacterMovementRefactor()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            Vector2 input = new Vector2(x, y).normalized;
            float velocity = (isWalking ? walkSpeed : runSpeed);

            bool isMovementBlockedByUi = (chatRegister.ChatWindows.Count > 0 && EventSystem.current.currentSelectedGameObject != null);
            _intendedMovement = isMovementBlockedByUi ? Vector2.zero : input;

            if (input.magnitude > 0)
                _currentMovement = Vector2.MoveTowards(_currentMovement, _intendedMovement, _movementCurve.Evaluate(Time.deltaTime * (_movementCurveMultiplier * velocity)));
            else
            {
                _currentMovement = Vector2.MoveTowards(_currentMovement, _intendedMovement, _movementCurve.Evaluate(Time.deltaTime * (Mathf.Pow(_movementCurveMultiplier, 2) * velocity)));
            }
            
            _absoluteMovement =
                _currentMovement.y * Vector3.Cross(camera.transform.right, Vector3.up).normalized +
                _currentMovement.x * Vector3.Cross(Vector3.up, camera.transform.forward).normalized;
            
            if (_absoluteMovement.magnitude > 0)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_absoluteMovement), Time.deltaTime * 10);
            
            characterController.Move(_absoluteMovement);
            
            float currentAnimatorSpeed = characterAnimator.GetFloat("Speed");
            float controllerSpeed = Mathf.Clamp01(_currentMovement.magnitude);

            float newSpeed = Mathf.Lerp(currentAnimatorSpeed, controllerSpeed, Time.deltaTime * (_movementCurve.Evaluate(Time.deltaTime) * 15));

            characterAnimator.SetFloat("Speed", newSpeed);
        }
        
        private void ProcessCharacterMovement()
        {
            // TODO: Implement gravity and grabbing
            // Calculate next movement
            // The vector is not normalized to allow for the input having potential rise and fall times
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            //Ignore movement controls when typing in chat
            if (chatRegister.ChatWindows.Count > 0 && EventSystem.current.currentSelectedGameObject != null)
                _intendedMovement = new Vector2(0, 0);
            else
                _intendedMovement = new Vector2(x, y).normalized * (isWalking ? walkSpeed : runSpeed);
            
            _currentMovement = Vector2.MoveTowards(_currentMovement, _intendedMovement, Time.deltaTime * (Mathf.Pow(ACCELERATION / 5f, 3) / 5));
            
            var movement = Vector3.zero;

            // Move the player
            if (_currentMovement != Vector2.zero)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                _absoluteMovement =
                    _currentMovement.y * Vector3.Cross(camera.transform.right, Vector3.up).normalized +
                    _currentMovement.x * Vector3.Cross(Vector3.up, camera.transform.forward).normalized;

                if (_intendedMovement != Vector2.zero)
                {
                    // Move. Whenever we move we also readjust the player's direction to the direction they are running in.
                    characterController.Move((_absoluteMovement + Physics.gravity * Time.deltaTime) * (Time.deltaTime / 3.5f));
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_absoluteMovement), Time.deltaTime * 10);
                }
                if (_intendedMovement == Vector2.zero)
                {
                    _absoluteMovement = Vector3.Lerp(_absoluteMovement, Vector3.zero, Time.deltaTime * 5);
                }

                movement = _absoluteMovement * Time.deltaTime;
            }

            characterController.Move(movement);

            // animation Speed is a proportion of maximum runSpeed, and we smoothly transitions the speed with the Lerp
            float currentAnimatorSpeed = characterAnimator.GetFloat("Speed");
            float controllerSpeed = Mathf.Clamp01(characterController.velocity.magnitude / runSpeed);

            float newSpeed = Mathf.Lerp(currentAnimatorSpeed, controllerSpeed, Time.deltaTime * 15);

            characterAnimator.SetFloat("Speed", newSpeed);
        }

        private void ForceHeightLevel()
        {
                transform.position = new Vector3(transform.position.x, heightOffGround, transform.position.z);
        }

        private void LateUpdate()
        {
            //Must be the local player to animate through here
            if (!isLocalPlayer)
            {
                return;
            }

            foreach (SimpleBodyPartLookAt part in LookAt)
            {
                part.MoveTarget();

                Vector3 forward = transform.TransformDirection(Vector3.forward).normalized;
                Vector3 toOther = (part.target.position - transform.position).normalized;

                Vector3 targetLookAt = part.target.position - part.transform.position;
                Quaternion targetRotation = Quaternion.FromToRotation(forward, targetLookAt.normalized);
                targetRotation = Quaternion.RotateTowards(part.currentRot, targetRotation, Time.deltaTime * part.rotationSpeed * Mathf.Rad2Deg);

                float targetAngle = Mathf.Abs(Quaternion.Angle(Quaternion.identity, targetRotation));
                if (targetAngle > part.minRotationLimit && targetAngle < part.maxRotationLimit)
                {
                    part.currentRot = targetRotation;
                }
                part.transform.localRotation = part.currentRot;
            }

            // TODO: Might eventually want more animation options. E.g. when in 0-gravity and 'clambering' via a surface
            //characterAnimator.SetBool("Floating", false); // Note: Player can be floating and still move
        }
    }
    
}