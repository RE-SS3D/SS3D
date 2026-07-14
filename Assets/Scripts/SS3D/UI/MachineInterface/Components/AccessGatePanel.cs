using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AccessGatePanel : VisualElement
    {
        public event Action SwipeRequested;

        private readonly VisualElement _scanOverlay;
        private readonly Label _headline;
        private readonly Label _subline;
        private readonly SteelButton _swipeButton;

        private string _lockedHeadline = "NO ID INSERTED";
        private string _lockedSubline = "Insert ID card to control connected systems";
        private string _scanningSubline = "Verifying engineering clearance";

        public AccessGatePanel()
        {
            AddToClassList("access-gate-panel");
            pickingMode = PickingMode.Position;

            VisualElement slot = new();
            slot.AddToClassList("access-gate-panel__slot");

            VisualElement slotClip = new();
            slotClip.AddToClassList("access-gate-panel__slot-clip");

            VisualElement slotLine = new();
            slotLine.AddToClassList("access-gate-panel__slot-line");
            slotClip.Add(slotLine);

            _scanOverlay = new VisualElement();
            _scanOverlay.AddToClassList("access-gate-panel__scan-overlay");
            _scanOverlay.style.display = DisplayStyle.None;
            slotClip.Add(_scanOverlay);

            slot.Add(slotClip);

            _headline = new Label("NO ID INSERTED");
            _headline.AddToClassList("access-gate-panel__headline");
            _headline.AddToClassList("font-titling");

            _subline = new Label("Insert ID card to control connected systems");
            _subline.AddToClassList("access-gate-panel__sub");
            _subline.AddToClassList("font-terminal");

            _swipeButton = new SteelButton { Text = "SWIPE ID CARD" };
            _swipeButton.AddToClassList("access-gate-panel__button");
            _swipeButton.Clicked += OnSwipeClicked;

            Add(slot);
            Add(_headline);
            Add(_subline);
            Add(_swipeButton);
        }

        [UxmlAttribute]
        public string GateHeadline
        {
            get => _headline.text;
            set => _headline.text = value;
        }

        [UxmlAttribute]
        public string GateSubline
        {
            get => _subline.text;
            set => _subline.text = value;
        }

        [UxmlAttribute]
        public string SwipeLabel
        {
            get => _swipeButton.Text;
            set => _swipeButton.Text = value;
        }

        [UxmlAttribute]
        public string LockedSubline
        {
            get => _lockedSubline;
            set
            {
                _lockedSubline = value;
                _subline.text = value;
            }
        }

        public void SetState(bool scanning)
        {
            _scanOverlay.style.display = scanning ? DisplayStyle.Flex : DisplayStyle.None;

            if (scanning)
            {
                GateHeadline = "Reading ID…";
                GateSubline = _scanningSubline;
                SwipeLabel = "Scanning…";
                _swipeButton.Disabled = true;
            }
            else
            {
                GateHeadline = _lockedHeadline;
                GateSubline = _lockedSubline;
                SwipeLabel = "SWIPE ID CARD";
                _swipeButton.Disabled = false;
            }
        }

        private void OnSwipeClicked()
        {
            SwipeRequested?.Invoke();
        }
    }
}
