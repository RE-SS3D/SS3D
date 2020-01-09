using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraRegister : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        cameraFollow.SetTarget(gameObject);
        cameraFollow.enabled = true;
    }
}
