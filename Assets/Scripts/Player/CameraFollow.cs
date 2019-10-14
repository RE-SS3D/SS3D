using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target = null;
    
    // the offset
    public float distance = 3f; // total distance from the target
    public float angle = 90f;    // horizontal angle of the camera (around the z axis)
    public float vAngle = 35f;  // angle above the player

    public float rotationSpeed = 110f; // rotation speed multiplier

    // Limits
    public float minDistance = 2f;
    public float maxDistance = 20f;
    
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
    }

    /**
     * Gather inputs used to determine new camera position
     */
    public void Update()
    {
        // input handling
        float zoom = Input.GetAxisRaw("Camera Zoom");
        if (Input.GetButton("Camera Rotation"))
        {
            angle += Input.GetAxis("Camera Rotation") * Time.deltaTime * rotationSpeed;
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
        Vector3 relativePosition = Quaternion.Euler(0, angle, vAngle) * new Vector3(distance, 0, 0);
        // Do some minor lerping to make the movement smoother. TODO: This smoothing needs tweaking, using slerps and/or other things to get camera movement really nice feeling
        currentPosition = Vector3.MoveTowards(currentPosition, relativePosition, Time.deltaTime * 10.0f);

        // Determine the part of the target we want to follow
        Vector3 targetPosition = target.transform.position + playerOffset;
        // Look at that part from the correct position
        this.transform.position = targetPosition + currentPosition;
        this.transform.LookAt(targetPosition);
    }

    private Vector3 currentPosition = new Vector3();
    private Vector3 playerOffset = new Vector3();
}
