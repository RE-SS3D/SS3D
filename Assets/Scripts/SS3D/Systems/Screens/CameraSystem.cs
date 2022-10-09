using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public class CameraSystem : SpessSystem
    {
        [SerializeField] private SpessBehaviour _playerCamera;

        public SpessBehaviour PlayerCamera => _playerCamera;
    }
}