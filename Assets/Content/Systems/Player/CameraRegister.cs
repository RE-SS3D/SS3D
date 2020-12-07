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
            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();
            cameraFollow.SetTarget(gameObject);
            cameraFollow.enabled = true;
        }
    }
}