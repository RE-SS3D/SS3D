using UnityEngine;
using Mirror;
using SS3D.Engine.Chat;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace SS3D.Content.Systems.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidMovementController : NetworkBehaviour
    {
        public const float ACCELERATION = 25f;

        // The base speed at which the given character can move
        [SyncVar] public float runSpeed = 5f;

        // The base speed for the character when walking. To disable walkSpeed, set it to runSpeed
        [SyncVar] public float walkSpeed = 2f;

        private Animator characterAnimator;
        private CharacterController characterController;
        private Camera camera;

        // Current movement the player is making.
        private Vector2 currentMovement = new Vector2();
        private Vector2 intendedMovement = new Vector2();
        public Vector3 absoluteMovement = new Vector3();

        private bool isWalking = false;
        //Required to detect if player is typing and stop accepting movement input
        private ChatRegister chatRegister;

        [SerializeField]
        private float heightOffGround = 0.1f;

        [SerializeField] SimpleBodyPartLookAt[] LookAt;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            characterAnimator = GetComponent<Animator>();
            chatRegister = GetComponent<ChatRegister>();
            camera = CameraManager.singleton.playerCamera; 
        }

        void Update()
        {

            //Must be the local player, or they cannot move
            if (!isLocalPlayer)
            {
                return;
            }

            //Ignore movement controls when typing in chat
            if (chatRegister.ChatWindow != null && chatRegister.ChatWindow.PlayerIsTyping())
            {
                currentMovement.Set(0, 0);
                return;
            }

            if (Input.GetButtonDown("Toggle Run"))
            {
                isWalking = !isWalking;
            }

            // TODO: Implement gravity and grabbing
            // Calculate next movement
            // The vector is not normalized to allow for the input having potential rise and fall times
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            // Smoothly transition to next intended movement
            intendedMovement = new Vector2(x, y).normalized * (isWalking ? walkSpeed : runSpeed);
            currentMovement = Vector2.MoveTowards(currentMovement, intendedMovement, Time.deltaTime * (Mathf.Pow(ACCELERATION / 5f, 3) / 5));
            // Move the player
            if (currentMovement != Vector2.zero)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                absoluteMovement =
                currentMovement.y * Vector3.Cross(camera.transform.right, Vector3.up).normalized +
                currentMovement.x * Vector3.Cross(Vector3.up, camera.transform.forward).normalized;

                if (intendedMovement != Vector2.zero)
                {
                   
                    // Move. Whenever we move we also readjust the player's direction to the direction they are running in.
                    characterController.Move((absoluteMovement + Physics.gravity * Time.deltaTime) * (Time.deltaTime / 3.5f));

                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(absoluteMovement), Time.deltaTime * 10);
                }
                if (intendedMovement == Vector2.zero)
                {
                    absoluteMovement = Vector3.Lerp(absoluteMovement, Vector3.zero, Time.deltaTime * 5);
                }
                characterController.Move(absoluteMovement * Time.deltaTime);
            }
            // animation Speed is a proportion of maximum runSpeed, and we smoothly transitions the speed with the Lerp
            float currentSpeed = characterAnimator.GetFloat("Speed");
            float newSpeed = Mathf.LerpUnclamped(currentSpeed, currentMovement.magnitude / runSpeed , Time.deltaTime * (isWalking ? walkSpeed : runSpeed) * 3);
            characterAnimator.SetFloat("Speed", newSpeed);

            ForceHeightLevel();
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