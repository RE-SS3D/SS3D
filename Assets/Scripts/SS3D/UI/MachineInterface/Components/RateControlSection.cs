using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class RateControlSection : VisualElement
    {
        public event Action<bool> ToggleChanged;

        public event Action<float> RateDeltaRequested;

        private readonly ToggleSwitch _toggle;
        private readonly VisualElement _fill;
        private readonly Label _currentLabel;
        private readonly Label _maxLabel;
        private float _currentKw;
        private float _maxKw = 10f;

        public RateControlSection()
        {
            AddToClassList("rate-control");

            VisualElement header = new();
            header.AddToClassList("rate-control__header");

            Label title = new("INPUT");
            title.name = "rate-title";
            title.AddToClassList("rate-control__title");
            title.AddToClassList("font-titling");

            _toggle = new ToggleSwitch { OnLabel = "ENABLED", OffLabel = "DISABLED", Horizontal = true };

            VisualElement headerRight = new();
            headerRight.AddToClassList("rate-control__header-right");
            headerRight.Add(_toggle);
            header.Add(title);
            header.Add(headerRight);

            VisualElement barRow = new();
            barRow.AddToClassList("rate-control__bar-row");

            VisualElement track = new();
            track.AddToClassList("rate-control__track");

            VisualElement trackClip = new();
            trackClip.AddToClassList("rate-control__track-clip");

            _fill = new VisualElement();
            _fill.AddToClassList("rate-control__fill");
            trackClip.Add(_fill);
            track.Add(trackClip);

            _currentLabel = new Label("0.0 kW");
            _currentLabel.AddToClassList("rate-control__current");
            _currentLabel.AddToClassList("font-terminal");

            barRow.Add(track);
            barRow.Add(_currentLabel);

            VisualElement stepperRow = new();
            stepperRow.AddToClassList("rate-control__stepper-row");

            Label stepperHint = new("Maximum rate");
            stepperHint.AddToClassList("rate-control__stepper-hint");
            stepperHint.AddToClassList("font-body");

            VisualElement stepper = new();
            stepper.AddToClassList("rate-control__stepper");

            Button decButton = new(() => RateDeltaRequested?.Invoke(-1f)) { text = "−" };
            decButton.AddToClassList("rate-control__stepper-button");
            decButton.AddToClassList("font-terminal");

            _maxLabel = new Label("10 kW");
            _maxLabel.AddToClassList("rate-control__max");
            _maxLabel.AddToClassList("font-terminal");

            Button incButton = new(() => RateDeltaRequested?.Invoke(1f)) { text = "+" };
            incButton.AddToClassList("rate-control__stepper-button");
            incButton.AddToClassList("font-terminal");

            stepper.Add(decButton);
            stepper.Add(_maxLabel);
            stepper.Add(incButton);
            stepperRow.Add(stepperHint);
            stepperRow.Add(stepper);

            Add(header);
            Add(barRow);
            Add(stepperRow);

            _toggle.ValueChanged += value => ToggleChanged?.Invoke(value);
        }

        [UxmlAttribute]
        public string SectionTitle
        {
            get => this.Q<Label>("rate-title")?.text ?? string.Empty;
            set
            {
                Label title = this.Q<Label>("rate-title");
                if (title != null)
                {
                    title.text = value;
                }
            }
        }

        public bool IsEnabled
        {
            get => _toggle.IsOn;
            set => _toggle.IsOn = value;
        }

        public float CurrentKw
        {
            get => _currentKw;
            set
            {
                _currentKw = value;
                _currentLabel.text = $"{value:0.0} kW";
                UpdateFill();
            }
        }

        public float MaxKw
        {
            get => _maxKw;
            set
            {
                _maxKw = Mathf.Max(0.01f, value);
                    _maxLabel.text = $"{_maxKw:0} kW";
                UpdateFill();
            }
        }

        public void SetAccentTone(StatusTone tone)
        {
            _fill.RemoveFromClassList("tone-success");
            _fill.RemoveFromClassList("tone-warning");
            _fill.RemoveFromClassList("tone-danger");
            _fill.RemoveFromClassList("tone-info");

            string toneClass = tone switch
            {
                StatusTone.Warning => "tone-warning",
                StatusTone.Danger => "tone-danger",
                _ => "tone-success",
            };
            _fill.AddToClassList(toneClass);
        }

        private void UpdateFill()
        {
            float pct = _maxKw > 0f ? Mathf.Clamp01(_currentKw / _maxKw) : 0f;
            _fill.style.width = new Length(pct * 100f, LengthUnit.Percent);
        }
    }
}
