using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class GlanceableStatusChip : VisualElement
    {
        private readonly Label _headline;
        private readonly StatusBadge _badge;
        private readonly Label _rightText;
        private StatusTone _tone = StatusTone.Success;

        public GlanceableStatusChip()
        {
            AddToClassList("glanceable-status-chip");

            VisualElement left = new();
            left.AddToClassList("glanceable-status-chip__left");

            _headline = new Label("STATUS");
            _headline.AddToClassList("glanceable-status-chip__headline");
            _headline.AddToClassList("font-titling");

            _badge = new StatusBadge { Text = "NOMINAL" };

            left.Add(_headline);
            left.Add(_badge);

            _rightText = new Label();
            _rightText.AddToClassList("glanceable-status-chip__right");
            _rightText.AddToClassList("font-terminal");
            _rightText.style.display = DisplayStyle.None;

            Add(left);
            Add(_rightText);

            ApplyTone(_tone);
        }

        [UxmlAttribute]
        public string Headline
        {
            get => _headline.text;
            set => _headline.text = value;
        }

        [UxmlAttribute]
        public string BadgeText
        {
            get => _badge.Text;
            set => _badge.Text = value;
        }

        [UxmlAttribute]
        public string RightText
        {
            get => _rightText.text;
            set
            {
                _rightText.text = value;
                _rightText.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
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

        public void SetContent(string headline, string badgeText, StatusTone tone, string rightText = null)
        {
            Headline = headline;
            BadgeText = badgeText;
            RightText = rightText ?? string.Empty;
            Tone = tone;
        }

        private void ApplyTone(StatusTone tone)
        {
            StatusToneUtility.ApplyTone(this, tone, background: true);
            StatusToneUtility.ApplyTone(_headline, tone);

            if (_badge != null)
            {
                _badge.Tone = tone;
            }
        }
    }
}
