using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps an armature collider to a gameplay body zone for combat and medical targeting.
    /// </summary>
    public class ZoneTargetCollider : MonoBehaviour
    {
        [SerializeField] private BodyZone _zone;

        public BodyZone Zone => _zone;
    }
}
