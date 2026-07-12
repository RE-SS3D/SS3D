using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class StatusDot : VisualElement
    {
        private StatusTone _tone = StatusTone.Success;

        public StatusDot()
        {
            AddToClassList("status-dot");
            ApplyTone(_tone);
        }

        [UxmlAttribute]
        public StatusTone Tone
        {
            get => _tone;
            set
            {
                _tone = value;
                ApplyTone(value);
            }
        }

        private void ApplyTone(StatusTone tone)
        {
            StatusToneUtility.ApplyTone(this, tone);
        }
    }
}
