using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Clothing
{
    /// <summary>
    /// Asset to quickly edit stuff and reference them easily
    /// TODO: Make this a list of possible clothes, so we can easily set clothing for monkeys, humans and other
    /// </summary>
    [CreateAssetMenu(fileName = "ClothingAsset", menuName = "Data/Inventory/ClothingAsset", order = 0)]
    public class ClothingAsset : ScriptableObject
    {
        /// <summary>
        /// Multiple clothing of the same thing to function with other armatures
        /// </summary>
        public List<ClothingObject> ClothingObject;
    }
}