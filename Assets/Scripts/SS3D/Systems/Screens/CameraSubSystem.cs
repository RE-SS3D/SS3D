using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public class CameraSubSystem : SubSystem
    {
        [SerializeField] private Actor _playerCamera;

        /// <summary>
        /// Gameplay camera Actor (typically <see cref="CameraFollow"/> on the scene Player Camera).
        /// Lazy: hub can Awake Online before Game loads the MainCamera.
        /// </summary>
        public Actor PlayerCamera
        {
            get
            {
                if (_playerCamera == null)
                {
                    TryResolvePlayerCamera();
                }

                return _playerCamera;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            TryResolvePlayerCamera();

#if UNITY_SERVER
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
#endif
        }

        private void TryResolvePlayerCamera()
        {
            if (_playerCamera != null)
            {
                return;
            }

            // Prefer tagged MainCamera (Player Camera prefab in Game). Hub spawn is before Game loads,
            // so this often no-ops until PlayerCamera is first read after the scene is Online.
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out Actor mainActor))
            {
                _playerCamera = mainActor;
                return;
            }

            CameraFollow follow = FindObjectOfType<CameraFollow>(true);
            if (follow != null)
            {
                _playerCamera = follow;
            }
        }
    }
}
