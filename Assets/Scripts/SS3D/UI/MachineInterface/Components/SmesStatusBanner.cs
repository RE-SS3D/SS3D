using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class SmesStatusBanner : VisualElement
    {
        private readonly Label _headline;
        private readonly StatusBadge _badge;
        private readonly VisualElement _connectionDot;
        private readonly Label _connectionText;

        public SmesStatusBanner()
        {
            AddToClassList("smes-status-banner");

            VisualElement left = new();
            left.AddToClassList("smes-status-banner__left");

            _headline = new Label("SMES ONLINE");
            _headline.AddToClassList("smes-status-banner__headline");
            _headline.AddToClassList("font-titling");

            _badge = new StatusBadge { Text = "ONLINE" };

            left.Add(_headline);
            left.Add(_badge);

            VisualElement right = new();
            right.AddToClassList("smes-status-banner__connection");

            _connectionDot = new VisualElement();
            _connectionDot.AddToClassList("smes-status-banner__connection-dot");

            _connectionText = new Label();
            _connectionText.AddToClassList("smes-status-banner__connection-text");
            _connectionText.AddToClassList("font-terminal");

            right.Add(_connectionDot);
            right.Add(_connectionText);

            Add(left);
            Add(right);
        }

        public void SetContent(string headline, string badgeText, string connectionText, StatusTone tone)
        {
            _headline.text = headline.ToUpperInvariant();
            _badge.Text = badgeText;
            _badge.Tone = tone;
            _connectionText.text = connectionText;

            StatusToneUtility.ApplyTone(this, tone, background: true);
            StatusToneUtility.ApplyTone(_headline, tone);
            StatusToneUtility.ApplyTone(_connectionDot, tone);
        }
    }
}
