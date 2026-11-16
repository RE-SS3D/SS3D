using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class StatusBadge : VisualElement
    {
        private readonly Label _label;
        private StatusTone _tone = StatusTone.Info;
        private string _text = "STATUS";

        public StatusBadge()
        {
            AddToClassList("status-badge");
            StatusToneUtility.ApplyTone(this, _tone);

            _label = new Label(_text);
            _label.AddToClassList("status-badge__label");
            _label.AddToClassList("font-arcade");
            Add(_label);
        }

        [UxmlAttribute]
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _label.text = value;
            }
        }

        [UxmlAttribute]
        public StatusTone Tone
        {
            get => _tone;
            set
            {
                _tone = value;
                StatusToneUtility.ApplyTone(this, value);
            }
        }
    }
}
