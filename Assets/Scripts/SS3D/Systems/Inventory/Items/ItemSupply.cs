using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Items
{
    [Serializable]
    public struct ItemSupply
    {
        [FormerlySerializedAs("type")]
        [SerializeField]
        private ItemSupplyType _type;

        [FormerlySerializedAs("drainRate")]
        [SerializeField]
        private int _drainRate;

        [FormerlySerializedAs("maxSupply")]
        [SerializeField]
        private int _maxSupply;

        [FormerlySerializedAs("currentSupply")]
        [SerializeField]
        private int _currentSupply;

        public ItemSupply(ItemSupplyType type, int drainRate, int maxSupply, int currentSupply)
        {
            _type = type;
            _drainRate = drainRate;
            _maxSupply = maxSupply;
            _currentSupply = currentSupply;
        }

        public ItemSupplyType Type => _type;

        public int DrainRate => _drainRate;

        public int MaxSupply => _maxSupply;

        public int CurrentSupply => _currentSupply;

        public ItemSupply WithNewSupplyValue(int newSupply)
        {
            return new ItemSupply(_type, _drainRate, _maxSupply, newSupply);
        }
    }
}
