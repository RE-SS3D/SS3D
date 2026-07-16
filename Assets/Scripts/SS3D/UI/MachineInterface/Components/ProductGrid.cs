using SS3D.UI.MachineInterface;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ProductGrid : VisualElement
    {
        public event Action<int> ProductSelected;

        private readonly VisualElement _grid;
        private readonly List<(int Index, string Name, int Stock, bool Locked, bool CanSelect)> _lastSignature = new();
        private bool _hasRendered;
        private int _lastVendingProductIndex = -1;

        public ProductGrid()
        {
            AddToClassList("product-grid");
            _grid = new VisualElement();
            _grid.AddToClassList("product-grid__items");
            Add(_grid);
        }

        public void SetProducts(IReadOnlyList<VendingProductViewData> products, int vendingProductIndex)
        {
            // The network layer refreshes this snapshot on every electricity tick (5x/sec), far more often
            // than the tray actually changes. Rebuilding every card from scratch on each refresh replaces the
            // clicked button mid-gesture, dropping the click. Skip the rebuild entirely when nothing changed.
            if (_hasRendered && !HasChanged(products, vendingProductIndex))
            {
                return;
            }

            RecordSignature(products, vendingProductIndex);
            _hasRendered = true;

            _grid.Clear();

            if (products == null)
            {
                return;
            }

            for (int i = 0; i < products.Count; i++)
            {
                VendingProductViewData product = products[i];
                ProductCard card = new();
                card.Clicked += index => ProductSelected?.Invoke(index);
                _grid.Add(card);
                card.SetProduct(
                    product.Index,
                    product.Name,
                    product.Stock,
                    product.Locked,
                    vendingProductIndex == product.Index,
                    product.CanSelect);
            }
        }

        private bool HasChanged(IReadOnlyList<VendingProductViewData> products, int vendingProductIndex)
        {
            int count = products?.Count ?? 0;
            if (vendingProductIndex != _lastVendingProductIndex || count != _lastSignature.Count)
            {
                return true;
            }

            for (int i = 0; i < count; i++)
            {
                VendingProductViewData product = products[i];
                (int Index, string Name, int Stock, bool Locked, bool CanSelect) previous = _lastSignature[i];

                if (previous.Index != product.Index
                    || previous.Name != product.Name
                    || previous.Stock != product.Stock
                    || previous.Locked != product.Locked
                    || previous.CanSelect != product.CanSelect)
                {
                    return true;
                }
            }

            return false;
        }

        private void RecordSignature(IReadOnlyList<VendingProductViewData> products, int vendingProductIndex)
        {
            _lastSignature.Clear();
            _lastVendingProductIndex = vendingProductIndex;

            if (products == null)
            {
                return;
            }

            for (int i = 0; i < products.Count; i++)
            {
                VendingProductViewData product = products[i];
                _lastSignature.Add((product.Index, product.Name, product.Stock, product.Locked, product.CanSelect));
            }
        }
    }
}
