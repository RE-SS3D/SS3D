using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class InventorySlot : VisualElement
    {
        private readonly VisualElement _well;
        private readonly Label _label;
        private readonly Label _unknownGlyph;
        private bool _unknown = true;
        private float _size = 48f;

        public InventorySlot()
        {
            AddToClassList("inventory-slot");

            _well = new VisualElement();
            _well.AddToClassList("inventory-slot__well");

            _unknownGlyph = new Label("?");
            _unknownGlyph.AddToClassList("inventory-slot__unknown-glyph");
            _unknownGlyph.AddToClassList("font-titling");
            _well.Add(_unknownGlyph);

            _label = new Label();
            _label.AddToClassList("inventory-slot__label");
            _label.AddToClassList("font-body");

            Add(_well);
            Add(_label);

            ApplySize(_size);
            ApplyUnknown(_unknown);
        }

        [UxmlAttribute]
        public bool Unknown
        {
            get => _unknown;
            set
            {
                _unknown = value;
                ApplyUnknown(value);
            }
        }

        [UxmlAttribute]
        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                ApplySize(value);
            }
        }

        [UxmlAttribute]
        public string SlotLabel
        {
            get => _label.text;
            set => _label.text = value;
        }

        private void ApplySize(float size)
        {
            _well.style.width = size;
            _well.style.height = size;
            _well.style.minWidth = size;
            _well.style.minHeight = size;
        }

        private void ApplyUnknown(bool unknown)
        {
            EnableInClassList("inventory-slot--unknown", unknown);
            _unknownGlyph.style.display = unknown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
