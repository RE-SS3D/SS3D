using Coimbra;
using SS3D.Systems.Inventory;
using UnityEngine;

namespace SS3D.Systems.Clothing
{
    public class ClothingSlot : InventorySlot
    {
        /// TODO: Make this a tickbox or something
        [SerializeField] private ClothingType _compatibleClothingType;
        /// <summary>
        /// Clothing instance parent
        /// </summary>
        [SerializeField] private Transform _clothingParent;
        /// <summary>
        /// Clothing object in this slot
        /// </summary>
        [SerializeField] private ClothingObject _clothingObject;
        /// <summary>
        /// Which weights the new clothing will copy from?
        /// </summary>
        [SerializeField] private SkinnedMeshRenderer _weightsCopyTarget;

        /// <summary>
        /// Adds the clothing and copies the bone weights
        /// </summary>
        /// <param name="clothingObject"></param>
        public void AddClothing(ClothingObject clothingObject)
        {
            if (clothingObject.ClothingType != _compatibleClothingType)
            {
                return;
            }

            RemoveClothing();

            _clothingObject = Instantiate(clothingObject, _clothingParent);
            _clothingObject.SetWeights(_weightsCopyTarget.rootBone, _weightsCopyTarget.bones);
        }

        /// <summary>
        /// Removes the clothing
        /// </summary>
        public void RemoveClothing()
        {
            _clothingObject.Destroy();
            _clothingObject = null;
        }
    }
}