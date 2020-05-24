using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Engine.FOV;
using Mirror;

namespace SS3D.Content.Systems.Player
{
    public class CameraRegister : NetworkBehaviour
    {
        public override void OnStartLocalPlayer()
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            cameraFollow.SetTarget(gameObject);
            cameraFollow.enabled = true;

            GameObject fovObject = GameObject.FindWithTag("FieldOfView");
            FieldOfView fovController = fovObject.GetComponent<FieldOfView>();
            fovController.target = transform;
            fovController.enabled = true;
        }
    }
}