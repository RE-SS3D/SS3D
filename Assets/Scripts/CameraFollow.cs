using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject cameraObj = null;
    public GameObject target = null;
    public Vector3 offset;
    private Quaternion rotationGoal = Quaternion.identity;
    private Vector3 cameraAngle;
    public float rotationTurnRate = 10f;

    void Start()
    {
        cameraObj.transform.localPosition = offset;
    }
    // Update is called once per frame
    void Update()
    {
        if (target !=  null && cameraObj != null)
        {
            transform.position = target.transform.position;
            cameraObj.transform.LookAt(target.transform);
        }

        if (Input.GetButtonDown("RotateCameraLeft"))
        {
            //transform.Rotate(0, 90.0f, 0);
            cameraAngle.y += 90.0f;
        }

        if (Input.GetButtonDown("RotateCameraRight"))
        {
            //transform.Rotate(0, -90.0f, 0);
            cameraAngle.y -= 90.0f;
        }
        rotationGoal = Quaternion.Euler(cameraAngle);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationGoal, rotationTurnRate * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotationGoal, rotationTurnRate * Time.deltaTime);
    }
}
