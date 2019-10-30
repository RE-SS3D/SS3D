﻿using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class MovementController : NetworkBehaviour
{
    public const float ACCELERATION = 25f;

    // The base speed at which the given character can move
    [SyncVar]
    public float runSpeed = 6f;
    // The base speed for the character when walking. To disable walkSpeed, set it to runSpeed
    [SyncVar]
    public float walkSpeed = 3f;

    private Animator characterAnimator;
    private CharacterController characterController;
    private new Camera camera;

    // Current movement the player is making.
    [HideInInspector]
    public Vector2 currentMovement = new Vector2();
    private bool isWalking = false;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = GetComponent<Animator>();
        camera = Camera.main;
    }

    void Update()
    {
        // Must be the local player, or they cannot move
        if (!isLocalPlayer) return;

        // Don't move if the player is ragdolled
        if (GetComponent<RagdollManager>().GetRagdolled()) return;

        if (Input.GetButtonDown("Toggle Run"))
            isWalking = !isWalking;
        
        // TODO: Get these values from the proper places they will be generated
        bool hasGravity = true;
        bool canGrabSomething = false;
        // TODO: Check other methods of moving, e.g. jetpack

        if ((hasGravity || canGrabSomething) && characterController.enabled)
        {
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
                Vector3 absoluteMovement = currentMovement.y * Vector3.Cross(camera.transform.right, Vector3.up).normalized + currentMovement.x * Vector3.Cross(Vector3.up, camera.transform.forward).normalized;
                // Move (without gravity). Whenever we move we also readjust the player's direction to the direction they are running in.
                characterController.Move(absoluteMovement * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(absoluteMovement);
            }
        }
        else
        {
            currentMovement.Set(0, 0);
        }

        // TODO: Might eventually want more animation options. E.g. when in 0-gravity and 'clambering' via a surface
        //characterAnimator.SetBool("Floating", !hasGravity); // Note: Player can be floating and still move
        characterAnimator.SetFloat("Speed", currentMovement.magnitude / runSpeed); // animation Speed is a proportion of maximum runSpeed
    }
}