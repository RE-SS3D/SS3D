using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class PresetModeButton : VisualElement
    {
        public event Action Clicked;

        private readonly Button _button;
        private readonly Label _title;
        private readonly Label _description;
        private bool _active;

        public PresetModeButton()
        {
            AddToClassList("preset-mode-button");

            _button = new Button(OnClicked);
            _button.AddToClassList("preset-mode-button__control");

            _title = new Label("Mode");
            _title.AddToClassList("preset-mode-button__title");
            _title.AddToClassList("font-titling");
            _title.pickingMode = PickingMode.Ignore;

            _description = new Label("Description");
            _description.AddToClassList("preset-mode-button__description");
            _description.AddToClassList("font-terminal");
            _description.pickingMode = PickingMode.Ignore;

            _button.Add(_title);
            _button.Add(_description);
            Add(_button);
        }

        [UxmlAttribute]
        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        [UxmlAttribute]
        public string Description
        {
            get => _description.text;
            set => _description.text = value;
        }

        [UxmlAttribute]
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                EnableInClassList("preset-mode-button--active", value);
                _button.EnableInClassList("preset-mode-button__control--active", value);
            }
        }

        [UxmlAttribute]
        public bool Disabled
        {
            get => !_button.enabledSelf;
            set => _button.SetEnabled(!value);
        }

        [UxmlAttribute]
        public PresetModeAccent Accent
        {
            get => PresetModeAccent.Default;
            set
            {
                _button.RemoveFromClassList("preset-mode-button__control--accent-rust");
                _button.RemoveFromClassList("preset-mode-button__control--accent-danger");
                _button.RemoveFromClassList("preset-mode-button__control--accent-info");
                _button.RemoveFromClassList("preset-mode-button__control--accent-steel");

                if (value != PresetModeAccent.Default)
                {
                    _button.AddToClassList($"preset-mode-button__control--accent-{value.ToString().ToLowerInvariant()}");
                }
            }
        }

        private void OnClicked()
        {
            Clicked?.Invoke();
        }
    }

    public enum PresetModeAccent
    {
        Default = 0,
        Rust = 1,
        Danger = 2,
        Info = 3,
        Steel = 4,
    }
}
