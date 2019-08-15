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
    private float floatingStartTime;
    private float bobbingDistance = 0.05f;
    private float floatingSpeed = 5.0f;
    private float floatingHeight = -1.987f;
    private bool floating = false;

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
        if (characterController.transform.position.y > floatingHeight)
        {
            characterController.Move(Vector3.down * gravity * Time.deltaTime);
            characterAnimator.SetFloat("Speed", new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude);
            floating = false;
        } 
        else
        {            
            if(!floating)
            {
                floating = true;
                floatingStartTime = Time.time;
                characterAnimator.SetFloat("Speed", Vector2.zero.magnitude);
            }
            var pos = characterController.transform.position;
            var floatPos = new Vector3(characterController.transform.position.x,
             floatingHeight - bobbingDistance - Mathf.Cos(Mathf.PI + (Time.time - floatingStartTime) * floatingSpeed) * bobbingDistance,
             characterController.transform.position.z);
            characterController.transform.position = floatPos;
        }
    }
}