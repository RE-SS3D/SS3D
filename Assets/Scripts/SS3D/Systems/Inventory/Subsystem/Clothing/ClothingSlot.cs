using Coimbra;
using SS3D.Core.Systems.Inventory;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Subsystem.Clothing
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
        /// Clothing instance
        /// </summary>
        [SerializeField] private ClothingObject clothingObject;

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

            this.clothingObject = Instantiate(clothingObject, _clothingParent);
            this.clothingObject.SetWeights(_weightsCopyTarget.rootBone, _weightsCopyTarget.bones);
        }

        /// <summary>
        /// Removes the clothing
        /// </summary>
        public void RemoveClothing()
        {
            clothingObject.Destroy();
            clothingObject = null;
        }
    }
}