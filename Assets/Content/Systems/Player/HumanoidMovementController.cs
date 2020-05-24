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
        private Camera mainCamera;

        // Current movement the player is making.
        private Vector2 currentMovement = new Vector2();
        private Vector2 intendedMovement = new Vector2();
        private Vector3 absoluteMovement = new Vector3();

        private bool isWalking = false;
        //Required to detect if player is typing and stop accepting movement input
        private ChatRegister chatRegister;
        private float heightOffGround;

        [SerializeField] private Transform chestBone;
        [SerializeField] private Transform chestIK;

        [SerializeField] private Transform headBone;
        [SerializeField] private Transform headIK;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            characterAnimator = GetComponent<Animator>();
            chatRegister = GetComponent<ChatRegister>();
            mainCamera = Camera.main;
            heightOffGround = transform.position.y;
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
            currentMovement = Vector2.Lerp(currentMovement, intendedMovement, Time.deltaTime * (Mathf.Pow(ACCELERATION / 9.5f, 3) / 6));

            // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.
            characterController.Move(absoluteMovement * Time.deltaTime);

            // Move the player
            if (currentMovement != Vector2.zero)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                Vector3 absoluteMovement =
                currentMovement.y * Vector3.Cross(mainCamera.transform.right, Vector3.up).normalized +
                currentMovement.x * Vector3.Cross(Vector3.up, mainCamera.transform.forward).normalized;

                if (intendedMovement != Vector2.zero)
                {
                   
                    //absoluteMovement = Vector3.Lerp(absoluteMovement, newAbsoluteMovement, Time.deltaTime * 2);
                    // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.
                    characterController.Move(absoluteMovement * Time.deltaTime);

                    // avoid unwanted rotation when you rotate the camera but isn't doing movement input, comment the "if" to see it

                    // Rotate the chest and head IKs objects
                    Quaternion newChestIKRotation = Quaternion.Lerp(chestIK.rotation, Quaternion.LookRotation(absoluteMovement), Time.deltaTime * (isWalking ? 3 : 10));
                    Quaternion newHeadIKRotation = Quaternion.Lerp(headIK.rotation, Quaternion.LookRotation(absoluteMovement), Time.deltaTime * (isWalking ? 15 : 5));

                    //float rotationDiference = Quaternion.
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(absoluteMovement), Time.deltaTime * (isWalking ? 5 : 7));
                    //Mathf.Pow(intendedMovement.magnitude, 2)

                    chestIK.rotation = newChestIKRotation;
                    headIK.rotation = newHeadIKRotation;
                }
                if (intendedMovement == Vector2.zero)
                {
                    Quaternion newChestIKRotation = Quaternion.Lerp(chestIK.rotation, transform.rotation, Time.deltaTime * 3);
                    Quaternion newHeadIKRotation = Quaternion.Lerp(headIK.rotation, transform.rotation, Time.deltaTime * 8);

                    absoluteMovement = Vector3.Lerp(absoluteMovement, Vector3.zero, Time.deltaTime * 5);
                    characterController.Move(absoluteMovement * Time.deltaTime);

                    chestIK.rotation = newChestIKRotation;
                    headIK.rotation = newHeadIKRotation;
                }
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

            // Limits the rotation of the bones, this is here because animations work on Update()
            if (chestBone.localRotation.y < 70 || chestBone.localRotation.y > -70)
                chestBone.RotateAroundLocal(Vector3.up, chestIK.localRotation.y);
        
            if (headBone.localRotation.y < 88 || headBone.localRotation.y > -88)
                headBone.RotateAroundLocal(Vector3.up, headIK.localRotation.y);
           
            // TODO: Might eventually want more animation options. E.g. when in 0-gravity and 'clambering' via a surface
            //characterAnimator.SetBool("Floating", false); // Note: Player can be floating and still move

            // animation Speed is a proportion of maximum runSpeed, and we smoothly transitions the speed with the Lerp

            float newSpeed = Mathf.LerpUnclamped(characterAnimator.GetFloat("Speed"), currentMovement.magnitude / runSpeed, Time.deltaTime * 35);
            characterAnimator.SetFloat("Speed", newSpeed);
        }
    }
    
}