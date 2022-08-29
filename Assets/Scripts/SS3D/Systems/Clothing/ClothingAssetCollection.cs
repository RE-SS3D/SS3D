using System.Collections.Generic;
using Coimbra;
using UnityEngine;

namespace SS3D.Systems.Clothing
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