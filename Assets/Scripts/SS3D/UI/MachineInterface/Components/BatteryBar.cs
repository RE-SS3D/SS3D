using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class BatteryBar : VisualElement
    {
        private readonly VisualElement _fill;
        private readonly Label _stateLabel;
        private readonly Label _percentLabel;
        private readonly Label _etaLabel;
        private float _value = 1f;

        public BatteryBar()
        {
            AddToClassList("battery-bar");

            VisualElement header = new();
            header.AddToClassList("battery-bar__header");

            Label title = new("CELL CHARGE");
            title.AddToClassList("battery-bar__title");
            title.AddToClassList("font-arcade");

            _stateLabel = new Label("CHARGED");
            _stateLabel.AddToClassList("battery-bar__state");
            _stateLabel.AddToClassList("font-arcade");
            _stateLabel.AddToClassList("battery-bar__state--success");

            header.Add(title);
            header.Add(_stateLabel);

            VisualElement track = new();
            track.AddToClassList("battery-bar__track");

            _fill = new VisualElement();
            _fill.AddToClassList("battery-bar__fill");
            track.Add(_fill);

            VisualElement footer = new();
            footer.AddToClassList("battery-bar__footer");

            _percentLabel = new Label("100%");
            _percentLabel.AddToClassList("battery-bar__percent");
            _percentLabel.AddToClassList("font-terminal");

            _etaLabel = new Label("—");
            _etaLabel.AddToClassList("battery-bar__eta");
            _etaLabel.AddToClassList("font-body");

            footer.Add(_percentLabel);
            footer.Add(_etaLabel);

            Add(header);
            Add(track);
            Add(footer);
        }

        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                _fill.style.width = new Length(_value * 100f, LengthUnit.Percent);
                _percentLabel.text = $"{Mathf.RoundToInt(_value * 100f)}%";
                UpdateFillTone();
            }
        }

        [UxmlAttribute]
        public string StateText
        {
            get => _stateLabel.text;
            set
            {
                _stateLabel.text = value;
                UpdateStateTone(value);
            }
        }

        [UxmlAttribute]
        public string EtaText
        {
            get => _etaLabel.text;
            set => _etaLabel.text = value;
        }

        private void UpdateFillTone()
        {
            _fill.RemoveFromClassList("battery-bar__fill--warning");
            _fill.RemoveFromClassList("battery-bar__fill--danger");

            if (_value <= 0.15f)
            {
                _fill.AddToClassList("battery-bar__fill--danger");
            }
            else if (_value <= 0.4f)
            {
                _fill.AddToClassList("battery-bar__fill--warning");
            }
        }

        private void UpdateStateTone(string stateText)
        {
            _stateLabel.RemoveFromClassList("battery-bar__state--success");
            _stateLabel.RemoveFromClassList("battery-bar__state--warning");
            _stateLabel.RemoveFromClassList("battery-bar__state--danger");

            string upper = stateText.ToUpperInvariant();
            if (upper.Contains("CRITICAL"))
            {
                _stateLabel.AddToClassList("battery-bar__state--danger");
            }
            else if (upper.Contains("DISCHARG"))
            {
                _stateLabel.AddToClassList("battery-bar__state--warning");
            }
            else
            {
                _stateLabel.AddToClassList("battery-bar__state--success");
            }
        }
    }
}
