using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine
{
    // This handles all the cameras in the game
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager singleton { get; private set; }

        public UnityEngine.Camera playerCamera;
        public UnityEngine.Camera examineCamera;

        private void Awake()
        {
            if (singleton != null) Destroy(gameObject);
            singleton = this;
        }
    }
}
