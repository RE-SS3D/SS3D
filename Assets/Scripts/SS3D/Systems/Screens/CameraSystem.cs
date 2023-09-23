using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public class CameraSystem : Core.Behaviours.System
    {
        [SerializeField]
        private Actor _playerCamera;

        public Actor PlayerCamera => _playerCamera;
    }
}