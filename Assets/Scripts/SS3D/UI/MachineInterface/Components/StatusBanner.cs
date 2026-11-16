using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class StatusBanner : VisualElement
    {
        private readonly Label _headline;
        private readonly StatusBadge _badge;
        private readonly Label _explanation;
        private StatusTone _tone = StatusTone.Success;

        public StatusBanner()
        {
            AddToClassList("status-banner");

            VisualElement header = new();
            header.AddToClassList("status-banner__header");

            _headline = new Label("POWER NOMINAL");
            _headline.AddToClassList("status-banner__headline");
            _headline.AddToClassList("font-titling");

            _badge = new StatusBadge { Text = "NOMINAL" };

            header.Add(_headline);
            header.Add(_badge);

            _explanation = new Label();
            _explanation.AddToClassList("status-banner__explanation");
            _explanation.AddToClassList("font-body");

            Add(header);
            Add(_explanation);

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
        public string Explanation
        {
            get => _explanation.text;
            set => _explanation.text = value;
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

        public void SetContent(string headline, string badgeText, string explanation, StatusTone tone)
        {
            Headline = headline;
            BadgeText = badgeText;
            Explanation = explanation;
            Tone = tone;
        }

        private void ApplyTone(StatusTone tone)
        {
            StatusToneUtility.ApplyTone(this, tone, background: true);

            if (_badge != null)
            {
                _badge.Tone = tone;
            }
        }
    }
}
