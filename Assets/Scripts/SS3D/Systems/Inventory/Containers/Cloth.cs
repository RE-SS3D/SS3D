using Coimbra;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Script to put on cloth, which are gameobjects going to the clothes slots.
    /// In the future, should be the folded models, and could contain a reference to the worn mesh version (and maybe torn mesh version, stuff like that...)
    /// </summary>
    public class Cloth : MonoBehaviour
    {
        [SerializeField]
        private SerializableDictionary<ClothType, Mesh> _clothMeshes;

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