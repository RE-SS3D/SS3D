using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target = null;
    
    // the offset
    public float height = 7f; // height from the target
    public float distance = 5f; // distance from the target
    public float angle = 0f; // and the camera angle

    // the input
    public float scrollSpeed = 2f; // scroll speed multiplier
    public float rotationSpeed = 110f; // rotation speed
    public float camera_angle = 62f;

    private float rotation = 0; // current rotation delta
    private float rotationLast = 0; // rotation memory 
    private float mouseX; // the previous X mouse position
    private float scrollWheel; // the scroll wheel
    private float currentAngle; // curent camera angle 

    private float input_timer; // track when button was pressed
    private Vector3 targetFollower = Vector3.zero;
    
    private void LateUpdate() 
    {
        mouseX = Input.mousePosition.x; // stores the previous X mouse position
    }

    void Update()
    {
        // if there is no target exit out of update
        if (!target)
        {
            return;
        }

        // Save previous rotation value
        rotationLast = rotation;

        // input handling
        scrollWheel = Input.GetAxisRaw("Mouse ScrollWheel");
        rotation = Input.GetAxisRaw("Camera Rotation");
        
        if (Input.GetButtonDown("Camera Rotation")) input_timer = Time.time;
        if (Input.GetButtonUp("Camera Rotation") && Time.time - input_timer < 0.3f)
        {
            angle += (rotationLast > 0 ? 1 :-1) * (angle % 90 == 0 ? 90 : 45);
            angle = Mathf.Round(angle / 90) * 90;
        }

        if (Input.GetButton("Camera Rotation") && Time.time - input_timer >= 0.3f)
        {
            angle += rotation * Time.deltaTime * rotationSpeed;
        }

        ZoomCap(); // limits the zoom

        //zooms in and out based on the scroll wheel
        distance -= scrollWheel * scrollSpeed;
        height -= scrollWheel * scrollSpeed;

        targetFollower = Vector3.Lerp(targetFollower, target.transform.position, Time.deltaTime * 8f);

        Vector3 worldPos = (Vector3.forward * -distance) + (Vector3.up * height); // calculates the camera position
        currentAngle = Mathf.LerpAngle(currentAngle, angle, Time.deltaTime * 8f);
        Vector3 camAngle = Quaternion.AngleAxis(currentAngle, Vector3.up) * worldPos; // calculates the camera angle
            
        // gets the flat position of the target 
        Vector3 flatTargetPos = new Vector3(targetFollower.x, 0f, targetFollower.z);
        this.transform.position = flatTargetPos + camAngle; // the final camera position 
        this.transform.LookAt(targetFollower);
    }

    //limits the zoom
    private void ZoomCap()
    {
        float minHeight = 3f; // the minimum height limit variable
        float maxHeight = 11f; // the maximum height limit variable

        float minDistance = 0.5f; // the minimum distance limit variable
        float maxDistance = 8.5f; // the maximum distance limit variable

        // the height limit
        if(height < minHeight) // the minimum height limit
        {
            height = minHeight;
        }
        else if(height > maxHeight) // the maximum height limit
        {
            height = maxHeight;
        }
            
        // the distance limit
        if (distance < minDistance) // the minimum distance limit
        {
            distance = minDistance;
        }
        else if(distance > maxDistance) // the maximum distance limit
        {
            distance = maxDistance;
        }
    }


}
