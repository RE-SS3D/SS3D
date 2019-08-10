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
    private bool rotation = false; // the rotation bool
    private float mouseX; // the previous X mouse position
    private float scrollWheel; // the scroll wheel
    
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
            // input handling
            scrollWheel = Input.GetAxisRaw("Mouse ScrollWheel");
            rotation = Input.GetButton("Camera Rotation"); 


            if (rotation) // is the Middle Mouse button pressed
            {
                if(Input.mousePosition.x != mouseX) // did the mouse move from the last position                  
                {
                    angle += (Input.mousePosition.x - mouseX) * 10f * Time.deltaTime; // recalculate the angle                        
                }
            }

            ZoomCap(); // limits the zoom

            //zooms in and out based on the scroll wheel
            distance -= scrollWheel * scrollSpeed;
            height -= scrollWheel * scrollSpeed;

            Vector3 worldPos = (Vector3.forward * -distance) + (Vector3.up * height); // calculates the camera position
            Vector3 camAngle = Quaternion.AngleAxis(angle, Vector3.up) * worldPos; // calculates the camera angle
            
            // gets the flat position of the target 
            Vector3 flatTargetPos = new Vector3(target.transform.position.x, 0f, target.transform.position.z);
            Vector3 finalPos = flatTargetPos + camAngle; // the final camera position 
            
            // smoothly goes to the final position
            this.transform.position = Vector3.Lerp(this.transform.position, finalPos, Time.deltaTime * 8f);
            this.transform.eulerAngles = new Vector3(62f, angle , 0f);
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
