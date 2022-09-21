using FishNet.Object;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Representation of a mind, it is what "owns" an entity (player controllable).
    ///
    /// A mind is controlled by a Soul, a Soul can control multiple minds (not at the same time).
    /// </summary>
    public class Mind : NetworkBehaviour
    {
        [SerializeField] private PlayerControllable _entity;
    }
}