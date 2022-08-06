using System;

namespace SS3D.Core.Systems.Inventory.Subsystem.Clothing
{
    [Serializable]
    public struct ClothingData
    {
        /// <summary>
        /// Clothing piece name (can be changed at runtime)
        /// </summary>
        public string Name;
        /// <summary>
        /// TODO: Add supported skeleton type
        /// </summary>
        public ClothingType ClothingType;
    }
}