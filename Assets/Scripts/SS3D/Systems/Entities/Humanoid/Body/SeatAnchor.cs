using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Anchor point for seat buckling interactions.
    /// Player must be within proximity and facing this transform to sit.
    /// </summary>
    public class SeatAnchor : NetworkBehaviour
    {
        [SerializeField] private Transform _sitTransform;

        public Transform SitTransform => _sitTransform != null ? _sitTransform : transform;

        private void Reset()
        {
            _sitTransform = transform;
        }
    }
}
