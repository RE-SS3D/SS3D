using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target = null;
    
    //the offset
    public float height = 7f;
    public float distance = 5f;
    public float angle = 0f;

    private enum CardinalDirection{North, East, South, West};

    private CardinalDirection cardinalDirection;

    // scroll speed multiplier
    public float scrollSpeed = 2f;

    // the scroll wheel

    public bool snapping = false;
    private bool rotation = false;
    private float mouseX;
    private float scrollWheel;    


    private void Start() 
    {
        cardinalDirection = CardinalDirection.South;
    }

    private void LateUpdate() 
    {
        mouseX = Input.mousePosition.x;
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
            snapping = Input.GetButton("Snapping");



            if (rotation)
            {
                if (Input.mousePosition.x != mouseX)
                {
                    if(snapping)
                    {

                        switch(cardinalDirection)
                        {
                            case CardinalDirection.South:
                                angle = 0;
                                cardinalDirection = CardinalDirection.East;
                            break;
                                
                            case CardinalDirection.East:
                                angle = 270;
                                cardinalDirection = CardinalDirection.North;
                            break;

                            case CardinalDirection.North:
                                angle = 180;
                                cardinalDirection = CardinalDirection.West;
                            break;

                            case CardinalDirection.West:
                                angle = 90;
                                cardinalDirection = CardinalDirection.South;
                            break;
                        }
                        
                    }
                    else
                    {
                        angle += (Input.mousePosition.x - mouseX) * 10f * Time.deltaTime;                        
                    }

                }

            }


            //limits the zoom
            ZoomCap();

            distance -= scrollWheel * scrollSpeed;
            height -= scrollWheel * scrollSpeed;

            Vector3 worldPos = (Vector3.forward * -distance) + (Vector3.up * height);
            Vector3 camAngle = Quaternion.AngleAxis(angle, Vector3.up) * worldPos;

            Vector3 flatTargetPos = new Vector3(target.transform.position.x, 0f, target.transform.position.z);

            Vector3 finalPos = flatTargetPos + camAngle;

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
