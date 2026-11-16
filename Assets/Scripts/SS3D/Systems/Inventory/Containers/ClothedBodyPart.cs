using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Used on bodypart of entities to indicate which type of clothes can be worn on them.
    /// </summary>
    public class ClothedBodyPart : MonoBehaviour
    {
        /// <summary>
        /// The type of clothes that can be worn on this body part.
        /// </summary>
        [SerializeField]
        private ClothType _clothType;

        /// <summary>
        /// The type of clothes that can be worn on this body part.
        /// </summary>
        public ClothType Type => _clothType;

        private Material[] _defaultMaterials;

        /// <summary>Restore the authored palette when changing from material-specific equipment.</summary>
        public void ApplyMaterials(SkinnedMeshRenderer renderer, Material[] materials)
        {
            _defaultMaterials ??= renderer.sharedMaterials;
            renderer.sharedMaterials = materials != null && materials.Length > 0 ? materials : _defaultMaterials;
        }
    }
}