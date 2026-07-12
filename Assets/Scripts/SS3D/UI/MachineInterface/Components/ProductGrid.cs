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

        public ProductGrid()
        {
            AddToClassList("product-grid");
            _grid = new VisualElement();
            _grid.AddToClassList("product-grid__items");
            Add(_grid);
        }

        public void SetProducts(IReadOnlyList<VendingProductViewData> products, int vendingProductIndex)
        {
            _grid.Clear();

            if (products == null)
            {
                return;
            }

            for (int i = 0; i < products.Count; i++)
            {
                VendingProductViewData product = products[i];
                ProductCard card = new();
                card.SetProduct(
                    product.Index,
                    product.Name,
                    product.Stock,
                    product.Locked,
                    vendingProductIndex == product.Index,
                    product.CanSelect);
                card.Clicked += index => ProductSelected?.Invoke(index);
                _grid.Add(card);
            }
        }
    }
}
