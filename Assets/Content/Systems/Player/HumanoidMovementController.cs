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

        private bool isWalking = false;

        private Animator characterAnimator;
        private CharacterController characterController;
        private Camera mainCamera;

        // Current movement the player is making.
        private Vector2 currentMovement = new Vector2();
        private Vector2 intendedMovement = new Vector2();
        private Vector3 absoluteMovement = new Vector3();

        //Required to detect if player is typing and stop accepting movement input
        private ChatRegister chatRegister;

        [SerializeField] private float heightOffGround = 0;

        [SerializeField] SimpleBodyPartLookAt[] LookAt;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            characterAnimator = GetComponent<Animator>();
            chatRegister = GetComponent<ChatRegister>();
            mainCamera = Camera.main;
        }

        void FixedUpdate()
        {
            ForceHeightLevel();

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

            absoluteMovement = new Vector3(absoluteMovement.x, 0, absoluteMovement.z);
            // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.
            //characterController.Move(absoluteMovement * Time.deltaTime);

            // Move the player
            if (currentMovement != Vector2.zero)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                // this also prevents moving without input
                Vector3 newAbsoluteMovement =
                currentMovement.y * Vector3.Cross(mainCamera.transform.right, Vector3.up).normalized +
                currentMovement.x * Vector3.Cross(Vector3.up, mainCamera.transform.forward).normalized;

                newAbsoluteMovement = new Vector3(newAbsoluteMovement.x, 0, newAbsoluteMovement.z);

                if (intendedMovement != Vector2.zero)
                {
                    absoluteMovement = Vector3.Lerp(absoluteMovement, newAbsoluteMovement, 8 / Time.deltaTime);
                    // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.

                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(newAbsoluteMovement), Time.deltaTime * ((isWalking ? walkSpeed : runSpeed) * 2));
                }

                if (intendedMovement == Vector2.zero)
                {
                    // avoid unwanted rotation when you rotate the camera but isn't doing movement input
                    absoluteMovement = Vector3.Lerp(absoluteMovement, Vector3.zero, Time.deltaTime);

                }
                characterController.Move(absoluteMovement * Time.deltaTime);
            }
        }

        private void ForceHeightLevel()
        {
            var currentPosition = transform.position;
            if (currentPosition.y > heightOffGround)
            {
                transform.position = new Vector3(currentPosition.x, heightOffGround, currentPosition.z);
            }
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

            // animation Speed is a proportion of maximum runSpeed, and we smoothly transitions the speed with the Lerp
            float currentSpeed = characterAnimator.GetFloat("Speed");
            float newSpeed = Mathf.LerpUnclamped(characterAnimator.GetFloat("Speed"), currentMovement.magnitude / runSpeed, Time.deltaTime * 30);
            characterAnimator.SetFloat("Speed", newSpeed);
        }
    }

}