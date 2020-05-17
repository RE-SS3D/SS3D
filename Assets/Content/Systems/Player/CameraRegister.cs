using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace SS3D.Content.Systems.Player
{
    public class CameraRegister : NetworkBehaviour
    {
        void Start()
        {
            Debug.Log(isLocalPlayer);
            if(!isLocalPlayer)
            {
                return;
            }
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            cameraFollow.SetTarget(gameObject);
            cameraFollow.enabled = true;
        }
    }
}