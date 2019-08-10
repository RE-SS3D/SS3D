using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class HumanMovement : NetworkBehaviour
{
    [SyncVar]
    private float gravity = 10f;
    [SyncVar]
    public float speed = 10f;
    [SyncVar]
    private float acceleration = 10f;

    private Animator characterAnimator;
    private CharacterController characterController;
    private Vector2 currentDirection;
    private new Camera camera;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = GetComponent<Animator>();
        camera = Camera.main;
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            // exit from update if this is not the local player
            return;
        }
        
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        currentDirection = Vector2.MoveTowards(currentDirection, input, acceleration * Time.deltaTime);
        

        //characterAnimator.SetFloat("Speed", 1f);
        if(currentDirection.magnitude > 0)
        {
            // Move relative to the camera
            Transform relativeMovement = camera.transform;
            if(relativeMovement != null)
            {
                var forward = Vector3.ProjectOnPlane(relativeMovement.forward, Vector3.up);
                forward.Normalize();
                var right = Vector3.ProjectOnPlane(relativeMovement.right, Vector3.up);
                right.Normalize();
                
                var dir = right * currentDirection.x + forward * currentDirection.y;
                characterController.Move(dir * Time.deltaTime * speed);
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        characterAnimator.SetFloat("Speed", new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude);
        characterController.Move(Vector3.down * gravity * Time.deltaTime); 
    }
}