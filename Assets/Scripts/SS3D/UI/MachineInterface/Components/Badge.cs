using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class Badge : VisualElement
    {
        private readonly Label _label;
        private StatusTone _tone = StatusTone.Success;
        private string _text = "STATUS";

        public Badge()
        {
            AddToClassList("diegetic-badge");
            StatusToneUtility.ApplyTone(this, _tone, background: true);

            _label = new Label(_text);
            _label.AddToClassList("diegetic-badge__label");
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
                StatusToneUtility.ApplyTone(this, value, background: true);
            }
        }
    }
}
