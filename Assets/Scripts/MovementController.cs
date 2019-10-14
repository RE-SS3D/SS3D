using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class MovementController : NetworkBehaviour
{
    // The base speed at which the given character can move
    [SyncVar]
    public float characterSpeed = 5f;

    private Animator characterAnimator;
    private CharacterController characterController;
    private new Camera camera;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = GetComponent<Animator>();
        camera = Camera.main;
    }

    void Update()
    {
        // Must be the local player, or they cannot move
        if (!isLocalPlayer)
            return;
        
        // TODO: Get these values from the proper places they will be generated
        bool hasGravity = true;
        bool canGrabSomething = false;
        // TODO: Check other methods of moving, e.g. jetpack

        Vector2 inputDirection = new Vector2();
        if (hasGravity || canGrabSomething) {
            // Calculate next movement
            // The vector is not normalized to allow for the input having potential rise and fall times
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            // The input direction ramps up to at most 1. The user accelerates based on input acceleration.
            inputDirection.Set(x, y);
            inputDirection.Normalize();
            inputDirection *= Mathf.Min(Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)), 1.0f);

            // Move the player
            if(inputDirection.magnitude > 0)
            {
                // Determine the absolute movement by aligning input to the camera's looking direction
                Vector3 absoluteMovement = inputDirection.y * Vector3.Cross(camera.transform.right, Vector3.up) + inputDirection.x * Vector3.Cross(Vector3.up, camera.transform.forward);
                // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.
                characterController.Move(absoluteMovement * characterSpeed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(absoluteMovement);
            }
        }
        else
        {
            inputDirection.Set(0, 0);
        }

        // TODO: Might eventually want more animation options. E.g. when in 0-gravity and 'clambering' via a surface
        characterAnimator.SetBool("Floating", !hasGravity); // Note: Player can be floating and still move
        characterAnimator.SetFloat("Speed", inputDirection.magnitude);
    }
}