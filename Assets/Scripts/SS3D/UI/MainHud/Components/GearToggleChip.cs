using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// An EVA switch beside the hands: hidden until its gear is worn, shows on or off, and a click
    /// toggles it unless the body cannot work it (Documents/design/armor.md §6).
    /// </summary>
    public sealed class GearToggleChip : VisualElement
    {
        private readonly Label _label;
        private readonly string _name;
        private bool _available;
        private bool _on;
        private bool _locked;

        public event Action Clicked;

        public GearToggleChip(Sprite icon, string name)
        {
            _name = name;
            AddToClassList("gear-chip");
            style.display = DisplayStyle.None;

            VisualElement well = new();
            well.AddToClassList("gear-chip__well");

            VisualElement glyph = new();
            glyph.AddToClassList("gear-chip__glyph");
            glyph.pickingMode = PickingMode.Ignore;
            if (icon != null)
            {
                glyph.style.backgroundImage = new StyleBackground(icon);
            }

            well.Add(glyph);

            VisualElement labelHost = new();
            labelHost.AddToClassList("gear-chip__label-host");
            labelHost.pickingMode = PickingMode.Ignore;

            _label = new Label();
            _label.AddToClassList("gear-chip__label");
            _label.AddToClassList("font-arcade");
            _label.pickingMode = PickingMode.Ignore;
            labelHost.Add(_label);

            Add(well);
            Add(labelHost);

            RegisterCallback<ClickEvent>(_ =>
            {
                if (!_locked)
                {
                    Clicked?.Invoke();
                }
            });
            Refresh();
        }

        public void SetState(bool available, bool on)
        {
            if (_available == available && _on == on)
            {
                return;
            }

            _available = available;
            _on = on;
            Refresh();
        }

        public void SetLocked(bool locked)
        {
            if (_locked == locked)
            {
                return;
            }

            _locked = locked;
            EnableInClassList("gear-chip--locked", locked);
        }

        private void Refresh()
        {
            style.display = _available ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("gear-chip--on", _on);
            _label.text = _on ? $"{_name} On" : $"{_name} Off";
        }
    }
}
