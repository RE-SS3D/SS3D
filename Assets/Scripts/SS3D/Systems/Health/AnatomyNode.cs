using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Granular anatomy node in the body-part tree. Severance hides this subtree on the character
    /// and spawns a world item copy via <see cref="HumanAnatomyController"/>.
    /// </summary>
    public class AnatomyNode : MonoBehaviour
    {
        [SerializeField] private BodyZone _primaryZone;
        [SerializeField] private bool _isDetachable;
        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        [SerializeField] private Transform _severAnchor;
        [SerializeField] private Item _severedDropPrefab;

        private bool _isSevered;

        public BodyZone PrimaryZone => _primaryZone;
        public bool IsDetachable => _isDetachable;
        public bool IsSevered => _isSevered;

        public Transform SeverAnchor => _severAnchor != null ? _severAnchor : transform;

        public Item ResolveSeveredDropPrefab()
        {
            if (_severedDropPrefab != null)
            {
                return _severedDropPrefab;
            }

            return GetComponent<Item>();
        }

        public void ApplySeveredVisuals()
        {
            _isSevered = true;

            if (_skinnedMeshRenderer != null)
            {
                _skinnedMeshRenderer.enabled = false;
            }

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }

            MeshRenderer[] staticRenderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < staticRenderers.Length; i++)
            {
                staticRenderers[i].enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = true;
            }
        }
    }
}
