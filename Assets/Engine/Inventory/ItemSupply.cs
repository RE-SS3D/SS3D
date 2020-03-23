using System;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    [Serializable]
    public struct ItemSupply
    {
        [SerializeField] private ItemSupplyType type;
        [SerializeField] private int drainRate;
        [SerializeField] private int maxSupply;
        [SerializeField] private int currentSupply;

        public ItemSupplyType Type => type;
        public int DrainRate => drainRate;
        public int MaxSupply => maxSupply;
        public int CurrentSupply => currentSupply;

        public ItemSupply(ItemSupplyType type, int drainRate, int maxSupply, int currentSupply)
        {
            this.type = type;
            this.drainRate = drainRate;
            this.maxSupply = maxSupply;
            this.currentSupply = currentSupply;
        }

        public ItemSupply WithNewSupplyValue(int newSupply)
        {
            return new ItemSupply(type, drainRate, maxSupply, newSupply);
        }
    }
}