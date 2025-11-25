using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Camera
{
    public class CameraSubSystem : Core.Behaviours.SubSystem
    {
        [SerializeField]
        private CameraActor _playerCamera;

        public CameraActor PlayerCamera => _playerCamera;
    }
}
