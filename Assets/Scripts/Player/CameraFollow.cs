using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public const float DISTANCE_ACCELERATION = 7.5f;
    public const float ROTATE_ACCELERATION = 90.0f;
    public const float DISTANCE_SCALING = 1.18f;

    // The object to follow
    public GameObject target = null;
    
    // the offset
    public float distance = 3f; // total distance from the target
    public float angle = 90f;    // horizontal angle of the camera (around the z axis)
    public float vAngle = 35f;  // angle above the player

    public float rotationSpeed = 110f; // rotation speed multiplier

    // Limits
    public float minDistance = 2f;
    public float maxDistance = 15f;
    
    /**
     * Updates the target the camera is meant to follow
     */
    public void SetTarget(GameObject target)
    {
        this.target = target;

        // Set the player height based on the character controller, if one is found
        var character = target.GetComponent<CharacterController>();
        if (character)
            playerOffset = new Vector3(0, target.GetComponent<CharacterController>().height);

        currentRotation = Quaternion.Euler(0, angle, vAngle);
    }

    /**
     * Gather inputs used to determine new camera position
     */
    public void Update()
    {
        // input handling
        float zoom = Input.GetAxis("Camera Zoom");
        if (Input.GetButton("Camera Rotation"))
        {
            angle += Input.GetAxisRaw("Camera Rotation") * Time.deltaTime * rotationSpeed;
        }

        // Change zoom based on scroll wheel, and ensure the distance is within the min and max limits
        distance = Mathf.Clamp(distance - zoom, minDistance, maxDistance);
    }

    /**
     * Determine camera position after any physics/player movement
     */
    public void LateUpdate()
    {
        // if there is no target exit out of update
        if (!target)
        {
            return;
        }

        // Determine position from the target by the angle and the distance
        // Smooth this value so that we get fluid motion around the sphere
        currentRotation = Quaternion.RotateTowards(currentRotation, Quaternion.Euler(0, angle, vAngle), Time.deltaTime * ROTATE_ACCELERATION);
        // Smooth the distance
        currentDistance = Mathf.MoveTowards(currentDistance, distance, Time.deltaTime * DISTANCE_ACCELERATION);

        Vector3 relativePosition = currentRotation * new Vector3(Mathf.Pow(DISTANCE_SCALING, currentDistance), 0, 0);

        // Determine the part of the target we want to follow
        Vector3 targetPosition = target.transform.position + playerOffset;
        // Look at that part from the correct position
        this.transform.position = targetPosition + relativePosition;
        this.transform.LookAt(targetPosition);
    }

    private Quaternion currentRotation;
    private float currentDistance;

    // Offset of target transform position to camera focus point.
    private Vector3 playerOffset = new Vector3();
}
