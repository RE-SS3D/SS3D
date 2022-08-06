using Coimbra;
using UnityEngine;

namespace SS3D.Core.Systems.Inventory.Subsystem.Clothing.Items
{
    /// <summary>
    /// Used to set clothing on an entity
    /// </summary>
    public class ClothingObject : MonoBehaviour
    {
        [SerializeField] private ClothingData _clothingData;

        [SerializeField] private Transform _clothingArmatureRoot;
        [SerializeField] private SkinnedMeshRenderer _clothingMesh;

        public ClothingType ClothingType => _clothingData.ClothingType;

        /// <summary>
        /// Sets clothing data according to saved one
        /// </summary>
        /// <param name="data"></param>
        public void SetClothingData(ClothingData data)
        {
            _clothingData = data;
        }

        /// <summary>
        ///  Sets weights on the clothing mesh
        /// </summary>
        /// <param name="rootBone"></param>
        /// <param name="bones"></param>
        public void SetWeights(Transform rootBone, Transform[] bones)
        {
            // The armature has to be visible on the editor lol,
            // Unity makes the mesh not show up in the preview if you destroy the armature in the prefab
            _clothingArmatureRoot.Destroy();

            _clothingMesh.rootBone = rootBone;
            _clothingMesh.bones = bones;
        }
    }
}