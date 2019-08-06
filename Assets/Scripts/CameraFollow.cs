using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target = null;
    public Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        if (target !=  null)
        {
            this.transform.position = target.transform.position + offset;
            this.transform.LookAt(target.transform);
        }
    }
}
