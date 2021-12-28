using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace SS3D.Content.Systems.Player
{
    public class CameraRegister : NetworkBehaviour
    {
        private Camera playerCamera;

        private void Start()
        {
            playerCamera = CameraManager.singleton.playerCamera;
            if(!isLocalPlayer) return;
            
            //camera.GetComponent<CameraFollow>().SetTarget(gameObject);
            CameraFollow cameraFollow = playerCamera.GetComponent<CameraFollow>();
    
            cameraFollow.SetTarget(gameObject);
            cameraFollow.enabled = true;
        }
    }
}