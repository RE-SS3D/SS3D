using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Subsystem.Clothing.Items
{
    /// <summary>
    /// Clothing collection used to hold all game clothing
    /// </summary>
    [CreateAssetMenu(fileName = "ClothingAssetCollection", menuName = "Data/Inventory/ClothingAssetCollection", order = 0)]
    public class ClothingAssetCollection : ScriptableSettings
    {
        public List<ClothingAsset> ClothingAssets;
    }
}