using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class CompactFilterToggle : VisualElement
    {
        public event Action<bool> ValueChanged;

        private readonly Label _label;
        private readonly VisualElement _track;
        private readonly VisualElement _thumb;
        private bool _isOn;

        public CompactFilterToggle()
        {
            AddToClassList("compact-filter-toggle");

            _track = new VisualElement();
            _track.AddToClassList("compact-filter-toggle__track");
            _track.RegisterCallback<ClickEvent>(_ => Toggle());

            _thumb = new VisualElement();
            _thumb.AddToClassList("compact-filter-toggle__thumb");
            _thumb.pickingMode = PickingMode.Ignore;
            _track.Add(_thumb);

            _label = new Label("O2");
            _label.AddToClassList("compact-filter-toggle__label");
            _label.AddToClassList("font-arcade");

            Add(_track);
            Add(_label);

            SetOn(false, notify: false);
        }

        [UxmlAttribute]
        public string FilterLabel
        {
            get => _label.text;
            set => _label.text = value;
        }

        [UxmlAttribute]
        public bool IsOn
        {
            get => _isOn;
            set => SetOn(value, notify: false);
        }

        [UxmlAttribute]
        public bool Locked
        {
            get => ClassListContains("compact-filter-toggle--locked");
            set
            {
                EnableInClassList("compact-filter-toggle--locked", value);
                _track.pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
            }
        }

        private void Toggle()
        {
            SetOn(!_isOn, notify: true);
        }

        private void SetOn(bool value, bool notify)
        {
            _isOn = value;
            EnableInClassList("compact-filter-toggle--on", value);
            _track.EnableInClassList("compact-filter-toggle__track--on", value);

            if (notify)
            {
                ValueChanged?.Invoke(value);
            }
        }
    }
}
