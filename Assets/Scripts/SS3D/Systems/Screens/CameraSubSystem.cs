using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public class CameraSubSystem : SubSystem
    {
        [SerializeField] private Actor _playerCamera;

        public Actor PlayerCamera => _playerCamera;

#if UNITY_SERVER
        protected override void OnAwake()
        {
            base.OnAwake();

            if (_playerCamera == null)
            {
                return;
            }

            GameObject cameraObject = _playerCamera.GameObject;

            if (cameraObject.TryGetComponent(out Camera camera))
            {
                camera.enabled = false;
            }

            if (cameraObject.TryGetComponent(out AudioListener audioListener))
            {
                audioListener.enabled = false;
            }
        }
#endif
    }
}