using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AccessGatePanel : VisualElement
    {
        public event Action SwipeRequested;

        private readonly VisualElement _slotClip;
        private readonly VisualElement _scanOverlay;
        private readonly VisualElement _deniedOverlay;
        private readonly Label _headline;
        private readonly Label _subline;
        private readonly SteelButton _swipeButton;

        private string _lockedHeadline = "NO ID INSERTED";
        private string _lockedSubline = "Insert ID card to control connected systems";
        private string _scanningSubline = "Verifying engineering clearance";
        private string _deniedSubline = "Card is not on the engineering access list";

        private IVisualElementScheduledItem _scanPulseSchedule;
        private IVisualElementScheduledItem _deniedFlashSchedule;
        private bool _scanPulseHigh;

        public AccessGatePanel()
        {
            AddToClassList("access-gate-panel");
            pickingMode = PickingMode.Position;

            VisualElement slot = new();
            slot.AddToClassList("access-gate-panel__slot");

            _slotClip = new VisualElement();
            _slotClip.AddToClassList("access-gate-panel__slot-clip");

            VisualElement slotLine = new();
            slotLine.AddToClassList("access-gate-panel__slot-line");
            _slotClip.Add(slotLine);

            _scanOverlay = new VisualElement();
            _scanOverlay.AddToClassList("access-gate-panel__scan-overlay");
            _scanOverlay.style.display = DisplayStyle.None;
            _slotClip.Add(_scanOverlay);

            _deniedOverlay = new VisualElement();
            _deniedOverlay.AddToClassList("access-gate-panel__denied-overlay");
            _deniedOverlay.style.display = DisplayStyle.None;
            _slotClip.Add(_deniedOverlay);

            slot.Add(_slotClip);

            _headline = new Label("NO ID INSERTED");
            _headline.AddToClassList("access-gate-panel__headline");
            _headline.AddToClassList("font-titling");

            _subline = new Label("Insert ID card to control connected systems");
            _subline.AddToClassList("access-gate-panel__sub");
            _subline.AddToClassList("font-terminal");

            _swipeButton = new SteelButton
            {
                Text = "SWIPE ID CARD",
                Variant = SteelButtonVariant.Blue,
            };
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

        public void SetState(bool scanning, bool denied = false)
        {
            StopAnimations();
            ClearToneClasses();

            if (scanning)
            {
                ApplyScanningState();
                return;
            }

            if (denied)
            {
                ApplyDeniedState();
                return;
            }

            ApplyLockedState();
        }

        private void ApplyScanningState()
        {
            _scanOverlay.style.display = DisplayStyle.Flex;
            _scanOverlay.style.opacity = 0.35f;
            _deniedOverlay.style.display = DisplayStyle.None;

            GateHeadline = "Reading ID…";
            GateSubline = _scanningSubline;
            SwipeLabel = "Scanning…";
            _swipeButton.Disabled = true;
            _swipeButton.Variant = SteelButtonVariant.Blue;

            _scanPulseHigh = false;
            _scanPulseSchedule = schedule.Execute(PulseScanOverlay).Every(350);
        }

        private void ApplyDeniedState()
        {
            _scanOverlay.style.display = DisplayStyle.None;
            _deniedOverlay.style.display = DisplayStyle.Flex;
            _deniedOverlay.style.opacity = 0.5f;

            GateHeadline = "Access Denied";
            GateSubline = _deniedSubline;
            SwipeLabel = "Swipe ID Card";
            _swipeButton.Disabled = false;
            _swipeButton.Variant = SteelButtonVariant.Danger;

            AddToClassList("tone-danger");
            _headline.AddToClassList("tone-danger");

            int flashCount = 0;
            _deniedFlashSchedule = schedule.Execute(() =>
            {
                flashCount++;
                _deniedOverlay.style.opacity = flashCount % 2 == 0 ? 0.5f : 0.15f;
                if (flashCount >= 4)
                {
                    _deniedFlashSchedule?.Pause();
                    _deniedFlashSchedule = null;
                    _deniedOverlay.style.opacity = 0.35f;
                }
            }).Every(250);
        }

        private void ApplyLockedState()
        {
            _scanOverlay.style.display = DisplayStyle.None;
            _deniedOverlay.style.display = DisplayStyle.None;

            GateHeadline = _lockedHeadline;
            GateSubline = _lockedSubline;
            SwipeLabel = "Swipe ID Card";
            _swipeButton.Disabled = false;
            _swipeButton.Variant = SteelButtonVariant.Blue;
        }

        private void PulseScanOverlay()
        {
            _scanPulseHigh = !_scanPulseHigh;
            _scanOverlay.style.opacity = _scanPulseHigh ? 0.5f : 0.25f;
        }

        private void StopAnimations()
        {
            _scanPulseSchedule?.Pause();
            _scanPulseSchedule = null;
            _deniedFlashSchedule?.Pause();
            _deniedFlashSchedule = null;
        }

        private void ClearToneClasses()
        {
            RemoveFromClassList("tone-danger");
            _headline.RemoveFromClassList("tone-success");
            _headline.RemoveFromClassList("tone-danger");
        }

        private void OnSwipeClicked()
        {
            SwipeRequested?.Invoke();
        }
    }
}
