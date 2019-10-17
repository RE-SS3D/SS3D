﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Sensitivities and Accelerations
    public const float DISTANCE_ACCELERATION = 15.0f; // How quickly distance changes
    public const float DISTANCE_SCALING = 1.18f; // The exponential effect of distance

    public const float HORIZONTAL_ROTATION_SENSITIVITY = 150f;
    public const float VERTICAL_ROTATION_SENSITIVITY = 80f;

    // Limits
    public const float MIN_VERTICAL_ANGLE = 10f;
    public const float MAX_VERTICAL_ANGLE = 80f;

    public const float MIN_DISTANCE = 2f;
    public const float MAX_DISTANCE = 15f;

    // The object to follow
    public GameObject target = null;
    
    // the offset
    public float distance = 3f; // total distance from the target
    public float angle = 90f;   // horizontal angle of the camera (around the z axis)
    public float vAngle = 35f;  // angle above the player
    
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
        float zoom = Input.GetAxis("Camera Zoom");
        float angleDelta = 0.0f;
        float vAngleDelta = 0.0f;

        // Camera buttons
        if (Input.GetButton("Camera Rotation"))
            angleDelta = Input.GetAxis("Camera Rotation") * HORIZONTAL_ROTATION_SENSITIVITY * Time.deltaTime;
        if (Input.GetButton("Camera Vertical Rotation"))
            vAngleDelta = Input.GetAxis("Camera Vertical Rotation") * VERTICAL_ROTATION_SENSITIVITY * Time.deltaTime;

        // Camera mouse movement: On right click being held down
        if (Input.GetMouseButton(1)) // There isnt a fucking enum for the mouse buttons
        {
            // Use mouse movement to determine axes
            angleDelta = Input.GetAxis("Mouse X");
            vAngleDelta = -Input.GetAxis("Mouse Y");
        }

        // Determine new values, clamping as necessary
        distance = Mathf.Clamp(distance - zoom, MIN_DISTANCE, MAX_DISTANCE);
        angle = (angle + angleDelta) % 360f;
        vAngle = Mathf.Clamp(vAngle + vAngleDelta, MIN_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);
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
        
        // Smooth the distance before using it
        currentDistance = Mathf.MoveTowards(currentDistance, distance, Time.deltaTime * DISTANCE_ACCELERATION);
        // The position is determined by the orientation and the distance, where distance has an exponential effect.
        Vector3 relativePosition = Quaternion.Euler(0, angle, vAngle) * new Vector3(Mathf.Pow(DISTANCE_SCALING, currentDistance), 0, 0);
        // Determine the part of the target we want to follow
        Vector3 targetPosition = target.transform.position + playerOffset;

        // Look at that part from the correct position
        this.transform.position = targetPosition + relativePosition;
        this.transform.LookAt(targetPosition);
    }

    private float currentDistance;

    // Offset of target transform position to camera focus point.
    private Vector3 playerOffset = new Vector3();
}
