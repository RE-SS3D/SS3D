using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Granular anatomy node in the body-part tree. Severance and attachment logic ships in later phases.
    /// </summary>
    public class AnatomyNode : MonoBehaviour
    {
        [SerializeField] private BodyZone _primaryZone;
        [SerializeField] private bool _isDetachable;

        public BodyZone PrimaryZone => _primaryZone;
        public bool IsDetachable => _isDetachable;
    }
}
