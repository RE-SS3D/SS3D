using UnityEngine;
using Mirror;
using SS3D.Engine.Chat;

namespace SS3D.Content.Code.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class MovementController : NetworkBehaviour
    {
        public const float ACCELERATION = 25f;

        // The base speed at which the given character can move
        [SyncVar] public float runSpeed = 5f;

        // The base speed for the character when walking. To disable walkSpeed, set it to runSpeed
        [SyncVar] public float walkSpeed = 2f;

        private Animator characterAnimator;
        private CharacterController characterController;
        private Camera mainCamera;

        // Current movement the player is making.
        private Vector2 currentMovement = new Vector2();
        private bool isWalking = false;
        //Required to detect if player is typing and stop accepting movement input
        private ChatRegister chatRegister;
        private float heightOffGround;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            characterAnimator = GetComponent<Animator>();
            chatRegister = GetComponent<ChatRegister>();
            mainCamera = Camera.main;
            heightOffGround = transform.position.y;
        }

        void Update()
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
            Vector2 intendedMovement = new Vector2(x, y).normalized * (isWalking ? walkSpeed : runSpeed);
            currentMovement = Vector2.MoveTowards(currentMovement, intendedMovement, Time.deltaTime * ACCELERATION);

            // Move the player
            if (currentMovement.magnitude > 0)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                Vector3 absoluteMovement =
                    currentMovement.y * Vector3.Cross(mainCamera.transform.right, Vector3.up).normalized +
                    currentMovement.x * Vector3.Cross(Vector3.up, mainCamera.transform.forward).normalized;
                // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.

                characterController.Move((absoluteMovement * Time.deltaTime));
                transform.rotation = Quaternion.LookRotation(absoluteMovement);
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
            
            // TODO: Might eventually want more animation options. E.g. when in 0-gravity and 'clambering' via a surface
            //characterAnimator.SetBool("Floating", false); // Note: Player can be floating and still move
            characterAnimator.SetFloat("Speed",
                currentMovement.magnitude / runSpeed); // animation Speed is a proportion of maximum runSpeed
        }
    }
}