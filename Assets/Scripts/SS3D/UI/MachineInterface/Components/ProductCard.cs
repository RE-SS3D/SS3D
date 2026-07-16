using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ProductCard : VisualElement
    {
        private const int VendPulseIntervalMs = 450;

        public event Action<int> Clicked;

        private readonly VisualElement _slotWrap;
        private readonly InventorySlot _slot;
        private readonly VisualElement _lockBadge;
        private readonly VisualElement _vendOverlay;
        private readonly VisualElement _vendDot;
        private readonly Label _nameLabel;
        private readonly Label _qtyLabel;
        private int _productIndex = -1;
        private IVisualElementScheduledItem _vendPulse;
        private bool _vendPulseVisible = true;

        public ProductCard()
        {
            AddToClassList("product-card");

            _slotWrap = new VisualElement();
            _slotWrap.AddToClassList("product-card__slot-wrap");

            _slot = new InventorySlot { Unknown = true, Size = 48 };
            _slotWrap.Add(_slot);

            _lockBadge = new VisualElement();
            _lockBadge.AddToClassList("product-card__lock-badge");
            _lockBadge.style.display = DisplayStyle.None;
            _slotWrap.Add(_lockBadge);

            _vendOverlay = new VisualElement();
            _vendOverlay.AddToClassList("product-card__vend-overlay");
            _vendDot = new VisualElement();
            _vendDot.AddToClassList("product-card__vend-dot");
            _vendOverlay.Add(_vendDot);
            _vendOverlay.style.display = DisplayStyle.None;
            _slotWrap.Add(_vendOverlay);

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
            RegisterCallback<DetachFromPanelEvent>(_ => StopVendPulse());
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
            EnableInClassList("product-card--out-of-stock", stock <= 0 && !locked);

            _lockBadge.style.display = locked ? DisplayStyle.Flex : DisplayStyle.None;
            _vendOverlay.style.display = vending ? DisplayStyle.Flex : DisplayStyle.None;

            if (vending)
            {
                StartVendPulse();
            }
            else
            {
                StopVendPulse();
            }
        }

        private void StartVendPulse()
        {
            if (_vendPulse != null)
            {
                return;
            }

            _vendPulseVisible = true;
            _vendDot.style.opacity = 1f;
            _vendPulse = _vendDot.schedule.Execute(ToggleVendPulse).Every(VendPulseIntervalMs);
        }

        private void StopVendPulse()
        {
            _vendPulse?.Pause();
            _vendPulse = null;
            _vendDot.style.opacity = 1f;
        }

        private void ToggleVendPulse()
        {
            _vendPulseVisible = !_vendPulseVisible;
            _vendDot.style.opacity = _vendPulseVisible ? 1f : 0.35f;
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
