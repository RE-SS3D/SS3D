using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Comms.UI
{
    /// <summary>
    /// Screen-space radio stack (middle-left) and all-station announcement banner (top-middle).
    /// Radio cards: radio-tower icon + channel header left, sender right on the top row, body below.
    /// </summary>
    public sealed class CommsFeedView
    {
        private const int MaxRadioCards = 6;
        private const float RadioFadeSeconds = 8f;
        private const float AnnounceFadeSeconds = 10f;

        private readonly StyleSheet _styleSheet;
        private readonly VectorImage _radioIcon;
        private readonly List<RadioCard> _radioCards = new();

        private VisualElement _root;
        private VisualElement _radioStack;
        private VisualElement _announceBanner;
        private Label _announceTitle;
        private Label _announceBody;
        private float _announceExpiry;

        private sealed class RadioCard
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Header;
            public Label Sender;
            public Label Body;
            public float Expiry;
        }

        public CommsFeedView(StyleSheet styleSheet, VectorImage radioIcon = null)
        {
            _styleSheet = styleSheet;
            _radioIcon = radioIcon;
        }

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement();
            _root.AddToClassList("comms-feed");
            _root.pickingMode = PickingMode.Ignore;
            if (_styleSheet != null)
            {
                _root.styleSheets.Add(_styleSheet);
            }

            _radioStack = new VisualElement();
            _radioStack.AddToClassList("comms-feed__radio-stack");
            _radioStack.pickingMode = PickingMode.Ignore;
            _root.Add(_radioStack);

            _announceBanner = new VisualElement();
            _announceBanner.AddToClassList("comms-feed__announce");
            _announceBanner.pickingMode = PickingMode.Ignore;
            _announceBanner.style.display = DisplayStyle.None;

            _announceTitle = new Label();
            _announceTitle.AddToClassList("comms-feed__announce-title");
            _announceBanner.Add(_announceTitle);

            _announceBody = new Label();
            _announceBody.AddToClassList("comms-feed__announce-body");
            _announceBanner.Add(_announceBody);

            _root.Add(_announceBanner);
            layerRoot.Add(_root);
        }

        public void Detach()
        {
            if (_announceBanner != null)
            {
                _announceBanner.UnregisterCallback<GeometryChangedEvent>(OnAnnounceGeometryChanged);
            }

            _root?.RemoveFromHierarchy();
            _root = null;
            _radioStack = null;
            _announceBanner = null;
            _announceTitle = null;
            _announceBody = null;
            _radioCards.Clear();
        }

        public void PushRadio(string header, string sender, string body, Color accent)
        {
            if (_radioStack == null)
            {
                return;
            }

            RadioCard card = AcquireRadioCard();
            card.Header.text = header ?? string.Empty;
            card.Sender.text = string.IsNullOrEmpty(sender) ? string.Empty : sender.ToUpperInvariant();
            card.Sender.style.display = string.IsNullOrEmpty(card.Sender.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            card.Body.text = body ?? string.Empty;
            card.Root.style.borderLeftColor = accent;
            card.Header.style.color = accent;
            if (card.Icon != null)
            {
                card.Icon.style.unityBackgroundImageTintColor = accent;
            }

            card.Expiry = Time.time + RadioFadeSeconds;
            card.Root.style.opacity = 1f;
            card.Root.style.display = DisplayStyle.Flex;
            _radioStack.Add(card.Root);

            while (_radioCards.Count > MaxRadioCards)
            {
                ReleaseOldestRadioCard();
            }
        }

        public void ShowAnnouncement(string title, string body)
        {
            if (_announceBanner == null)
            {
                return;
            }

            _announceBanner.UnregisterCallback<GeometryChangedEvent>(OnAnnounceGeometryChanged);
            _announceTitle.text = title ?? "ALL-STATION";
            _announceBody.text = body ?? string.Empty;
            _announceExpiry = Time.time + AnnounceFadeSeconds;
            _announceBanner.style.opacity = 1f;
            _announceBanner.style.display = DisplayStyle.Flex;
            _announceBanner.RegisterCallback<GeometryChangedEvent>(OnAnnounceGeometryChanged);
            CenterAnnounceBanner();
        }

        private void OnAnnounceGeometryChanged(GeometryChangedEvent evt)
        {
            CenterAnnounceBanner();
        }

        private void CenterAnnounceBanner()
        {
            if (_announceBanner == null || _root == null)
            {
                return;
            }

            float panelWidth = _root.resolvedStyle.width;
            float bannerWidth = _announceBanner.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || float.IsNaN(bannerWidth) || panelWidth <= 0f || bannerWidth <= 0f)
            {
                return;
            }

            _announceBanner.style.left = (panelWidth - bannerWidth) * 0.5f;
        }

        public void Tick()
        {
            float now = Time.time;
            for (int i = _radioCards.Count - 1; i >= 0; i--)
            {
                RadioCard card = _radioCards[i];
                float remaining = card.Expiry - now;
                if (remaining <= 0f)
                {
                    card.Root.RemoveFromHierarchy();
                    card.Root.style.display = DisplayStyle.None;
                    _radioCards.RemoveAt(i);
                    continue;
                }

                if (remaining < 1.5f)
                {
                    card.Root.style.opacity = remaining / 1.5f;
                }
            }

            if (_announceBanner != null && _announceBanner.style.display != DisplayStyle.None)
            {
                float remaining = _announceExpiry - now;
                if (remaining <= 0f)
                {
                    _announceBanner.UnregisterCallback<GeometryChangedEvent>(OnAnnounceGeometryChanged);
                    _announceBanner.style.display = DisplayStyle.None;
                }
                else if (remaining < 1.5f)
                {
                    _announceBanner.style.opacity = remaining / 1.5f;
                }
            }
        }

        private RadioCard AcquireRadioCard()
        {
            RadioCard card = new()
            {
                Root = new VisualElement(),
            };
            card.Root.AddToClassList("comms-feed__radio-card");
            card.Root.pickingMode = PickingMode.Ignore;

            VisualElement topRow = new VisualElement();
            topRow.AddToClassList("comms-feed__radio-top");
            topRow.pickingMode = PickingMode.Ignore;

            VisualElement headerCluster = new VisualElement();
            headerCluster.AddToClassList("comms-feed__radio-header-cluster");
            headerCluster.pickingMode = PickingMode.Ignore;

            card.Icon = new VisualElement();
            card.Icon.AddToClassList("comms-feed__radio-icon");
            card.Icon.pickingMode = PickingMode.Ignore;
            if (_radioIcon != null)
            {
                card.Icon.style.backgroundImage = new StyleBackground(_radioIcon);
            }

            headerCluster.Add(card.Icon);

            card.Header = new Label();
            card.Header.AddToClassList("comms-feed__radio-header");
            headerCluster.Add(card.Header);

            topRow.Add(headerCluster);

            card.Sender = new Label();
            card.Sender.AddToClassList("comms-feed__radio-sender");
            topRow.Add(card.Sender);

            card.Root.Add(topRow);

            card.Body = new Label();
            card.Body.AddToClassList("comms-feed__radio-body");
            card.Root.Add(card.Body);

            _radioCards.Add(card);
            return card;
        }

        private void ReleaseOldestRadioCard()
        {
            if (_radioCards.Count == 0)
            {
                return;
            }

            RadioCard oldest = _radioCards[0];
            oldest.Root.RemoveFromHierarchy();
            _radioCards.RemoveAt(0);
        }
    }
}
