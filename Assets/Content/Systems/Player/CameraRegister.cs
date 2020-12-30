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
        }

        public override void OnStartLocalPlayer()
        {
            if (camera == null) camera = CameraManager.singleton.playerCamera;
            
            if (NetworkClient.active)
            {
                if (NetworkClient.connection.identity.gameObject != transform.parent.gameObject)
                {
                    // Destroy if listener of other player
                    Destroy(gameObject);
                }
            }
            else if (NetworkServer.active)
            {
                // Destroy if server only
                Destroy(gameObject);
            }
            
            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();
            
            if (cameraFollow.target != null)
            {
                cameraFollow.SetTarget(gameObject);
                cameraFollow.enabled = true;
            }
        }
    }
}