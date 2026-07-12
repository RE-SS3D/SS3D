using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ProductCard : VisualElement
    {
        public event Action<int> Clicked;

        private readonly VisualElement _slotWrap;
        private readonly InventorySlot _slot;
        private readonly Label _nameLabel;
        private readonly Label _qtyLabel;
        private int _productIndex = -1;

        public ProductCard()
        {
            AddToClassList("product-card");

            _slotWrap = new VisualElement();
            _slotWrap.AddToClassList("product-card__slot-wrap");

            _slot = new InventorySlot { Unknown = true, Size = 48 };
            _slotWrap.Add(_slot);

            _nameLabel = new Label();
            _nameLabel.AddToClassList("product-card__name");
            _nameLabel.AddToClassList("font-body");

            _qtyLabel = new Label();
            _qtyLabel.AddToClassList("product-card__qty");
            _qtyLabel.AddToClassList("font-terminal");

            Add(_slotWrap);
            Add(_nameLabel);
            Add(_qtyLabel);

            RegisterCallback<ClickEvent>(OnClick);
        }

        public void SetProduct(
            int productIndex,
            string name,
            int stock,
            bool locked,
            bool vending,
            bool canSelect)
        {
            _productIndex = productIndex;
            _slot.SlotLabel = string.Empty;
            _nameLabel.text = name;

            if (locked)
            {
                _qtyLabel.text = "ID REQUIRED";
                _qtyLabel.style.color = new StyleColor(new UnityEngine.Color(0.36f, 0.35f, 0.33f));
                _qtyLabel.EnableInClassList("product-card__qty--out-of-stock", false);
            }
            else if (stock <= 0)
            {
                _qtyLabel.text = $"x {stock}";
                _qtyLabel.style.color = new StyleColor(new UnityEngine.Color(0.36f, 0.35f, 0.33f));
                _qtyLabel.EnableInClassList("product-card__qty--out-of-stock", true);
            }
            else
            {
                _qtyLabel.text = $"x {stock}";
                _qtyLabel.style.color = new StyleColor(new UnityEngine.Color(0.6f, 0.59f, 0.56f));
                _qtyLabel.EnableInClassList("product-card__qty--out-of-stock", false);
            }

            _nameLabel.style.color = stock <= 0 || locked
                ? new StyleColor(new UnityEngine.Color(0.36f, 0.35f, 0.33f))
                : new StyleColor(new UnityEngine.Color(0.91f, 0.9f, 0.89f));

            EnableInClassList("product-card--disabled", !canSelect);
            EnableInClassList("product-card--vending", vending);
            EnableInClassList("product-card--locked", locked);
        }

        private void OnClick(ClickEvent evt)
        {
            if (_productIndex < 0 || ClassListContains("product-card--disabled"))
            {
                return;
            }

            Clicked?.Invoke(_productIndex);
            evt.StopPropagation();
        }
    }
}
