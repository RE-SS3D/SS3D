using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Furniture
{
    [System.Serializable]
    public class VendingMachineProductStock
    {
        [FormerlySerializedAs("Product")]
        [SerializeField]
        private ItemObjectSo _product;

        [FormerlySerializedAs("Stock")]
        [SerializeField]
        private int _stock;

        public ItemObjectSo Product => _product;

        public int Stock
        {
            get => _stock;
            set => _stock = value;
        }
    }
}
