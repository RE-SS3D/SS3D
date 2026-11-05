using Coimbra;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Worn body-mesh map for cloth-slot Items. Pair with <see cref="ClothingItemPresentation"/>
    /// for the folded world/hand child; <see cref="ClothesDisplayer"/> applies these meshes to
    /// <see cref="ClothedBodyPart"/> while the Item is hidden in a worn slot.
    /// </summary>
    public class Cloth : MonoBehaviour
    {
        [SerializeField]
        private SerializableDictionary<ClothType, Mesh> _clothMeshes;

        [SerializeField]
        private Material[] _wornMaterials;

        [SerializeField]
        private string[] _occludedBodyRenderers;

        /// <summary>Opaque equipment can conceal underlying body/clothing surfaces to prevent clipping.</summary>
        public string[] OccludedBodyRenderers => _occludedBodyRenderers;

        /// <summary>Optional per-item materials; empty preserves the body's palette material.</summary>
        public Material[] WornMaterials => _wornMaterials;

        /// <summary>
        /// Get the mesh associated to a cloth type.
        /// </summary>
        /// <param name="clothType">type of cloth for which mesh is required</param>
        /// <returns>Mesh of the required cloth type</returns>
        public Mesh GetClothMesh(ClothType clothType)
        {
            if (_clothMeshes.TryGetValue(clothType, out Mesh mesh))
            {
                return mesh;
            }

            Log.Error(this, "Cloth Mesh not found");

            return null;
        }
    }
}