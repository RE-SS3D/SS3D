using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace SS3D.Content.Systems.Player
{
    public class CameraRegister : NetworkBehaviour
    {
        private Camera camera;

        private void Start()
        {
            camera = CameraManager.singleton.playerCamera;
            if(!isLocalPlayer) return;

            CameraManager.singleton.lobbyCamera.gameObject.SetActive(false);
            CameraManager.singleton.playerCamera.gameObject.SetActive(true);
            
            //camera.GetComponent<CameraFollow>().SetTarget(gameObject);
            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();

            cameraFollow.SetTarget(gameObject);
            cameraFollow.enabled = true;
        }
    }
}