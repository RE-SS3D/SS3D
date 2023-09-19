using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace SS3D.Engine.Utilities
{
    public class LookAtCamera : MonoBehaviour
    {
        /// <summary>
        /// If set to true, rotates the object by 180 degrees
        /// </summary>
        public bool RotateAround = false;
        private new Transform camera;
    
        void Start ()
        {
            Debug.Assert(Camera.main != null, "Camera.main != null");
            camera = Camera.main.transform;
        }
    
        void Update()
        {
            transform.LookAt(camera);
            if (RotateAround)
            {
                transform.Rotate(0, 180, 0);
            }
        }
    }
}
