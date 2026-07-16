using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class NumericStepper : VisualElement
    {
        public event Action<float> DeltaRequested;

        private readonly Label _hint;
        private readonly Label _value;
        private readonly Button _decButton;
        private readonly Button _incButton;

        public NumericStepper()
        {
            AddToClassList("numeric-stepper");

            VisualElement labels = new();
            labels.AddToClassList("numeric-stepper__labels");

            _hint = new Label();
            _hint.AddToClassList("numeric-stepper__hint");
            _hint.AddToClassList("font-body");
            _hint.style.display = DisplayStyle.None;

            labels.Add(_hint);

            _decButton = new Button(() => DeltaRequested?.Invoke(-1f)) { text = "−" };
            _decButton.AddToClassList("numeric-stepper__button");
            _decButton.AddToClassList("font-terminal");

            _value = new Label("0");
            _value.AddToClassList("numeric-stepper__value");
            _value.AddToClassList("font-terminal");

            _incButton = new Button(() => DeltaRequested?.Invoke(1f)) { text = "+" };
            _incButton.AddToClassList("numeric-stepper__button");
            _incButton.AddToClassList("font-terminal");

            VisualElement controls = new();
            controls.AddToClassList("numeric-stepper__controls");
            controls.Add(_decButton);
            controls.Add(_value);
            controls.Add(_incButton);

            Add(labels);
            Add(controls);
        }

        [UxmlAttribute]
        public string HintText
        {
            get => _hint.text;
            set
            {
                _hint.text = value;
                _hint.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute]
        public string ValueText
        {
            get => _value.text;
            set => _value.text = value;
        }

        [UxmlAttribute]
        public bool Locked
        {
            get => ClassListContains("numeric-stepper--locked");
            set
            {
                EnableInClassList("numeric-stepper--locked", value);
                _decButton.SetEnabled(!value);
                _incButton.SetEnabled(!value);
            }
        }
    }
}
