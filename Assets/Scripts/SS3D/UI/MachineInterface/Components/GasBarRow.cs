using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class GasBarRow : VisualElement
    {
        private readonly Label _label;
        private readonly VisualElement _trackClip;
        private readonly VisualElement _fill;
        private readonly Label _value;
        private float _fillPct;

        public GasBarRow()
        {
            AddToClassList("gas-bar-row");

            _label = new Label("O2");
            _label.AddToClassList("gas-bar-row__label");
            _label.AddToClassList("font-terminal");

            VisualElement track = new();
            track.AddToClassList("gas-bar-row__track");

            _trackClip = new();
            _trackClip.AddToClassList("gas-bar-row__track-clip");

            _fill = new VisualElement();
            _fill.AddToClassList("gas-bar-row__fill");

            _trackClip.Add(_fill);
            track.Add(_trackClip);

            _value = new Label("0.0%");
            _value.AddToClassList("gas-bar-row__value");
            _value.AddToClassList("font-terminal");

            Add(_label);
            Add(track);
            Add(_value);

            _trackClip.RegisterCallback<GeometryChangedEvent>(_ => UpdateFillWidth());
            RegisterCallback<AttachToPanelEvent>(_ => schedule.Execute(UpdateFillWidth));
            RegisterCallback<GeometryChangedEvent>(_ => UpdateFillWidth());
        }

        [UxmlAttribute]
        public string GasLabel
        {
            get => _label.text;
            set => _label.text = value;
        }

        [UxmlAttribute]
        public string ValueText
        {
            get => _value.text;
            set => _value.text = value;
        }

        [UxmlAttribute]
        public float FillPct
        {
            get => _fillPct;
            set
            {
                _fillPct = Mathf.Clamp(value, 0f, 100f);
                UpdateFillWidth();
            }
        }

        private void UpdateFillWidth()
        {
            float percent = Mathf.Clamp(_fillPct, 0f, 100f);
            float trackWidth = _trackClip.resolvedStyle.width;
            if (trackWidth > 0.5f)
            {
                _fill.style.width = trackWidth * (percent / 100f);
                return;
            }

            _fill.style.width = new Length(percent, LengthUnit.Percent);
        }

        [UxmlAttribute]
        public StatusTone ValueTone
        {
            get => StatusTone.Info;
            set => StatusToneUtility.ApplyTone(_value, value);
        }

        [UxmlAttribute]
        public StatusTone BarTone
        {
            get => StatusTone.Info;
            set
            {
                _fill.RemoveFromClassList("tone-success");
                _fill.RemoveFromClassList("tone-warning");
                _fill.RemoveFromClassList("tone-danger");
                _fill.RemoveFromClassList("tone-info");
                _fill.RemoveFromClassList("tone-neutral");
                _fill.RemoveFromClassList("gas-bar-row__fill--accent");

                if (value == StatusTone.Success)
                {
                    _fill.AddToClassList("gas-bar-row__fill--accent");
                }
                else if (value != StatusTone.Info)
                {
                    StatusToneUtility.ApplyTone(_fill, value);
                }
            }
        }
    }
}
