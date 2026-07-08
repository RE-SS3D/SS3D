using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class HoldRevealButton : VisualElement
    {
        public event Action Revealed;

        private readonly VisualElement _progressFill;
        private readonly IVisualElementScheduledItem _holdTicker;
        private bool _isHolding;
        private float _holdProgress;

        public HoldRevealButton()
        {
            AddToClassList("hold-reveal-button");

            _progressFill = new VisualElement();
            _progressFill.AddToClassList("hold-reveal-button__progress");

            Label label = new("Hold to access advanced diagnostics");
            label.AddToClassList("hold-reveal-button__label");
            label.AddToClassList("font-arcade");

            Add(_progressFill);
            Add(label);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<PointerCaptureOutEvent>(_ => CancelHold());

            _holdTicker = schedule.Execute(TickHold).Every(30);
            _holdTicker.Pause();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isHolding = true;
            _holdProgress = 0f;
            UpdateProgress();
            _holdTicker.Resume();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            CancelHold();
            evt.StopPropagation();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            CancelHold();
        }

        private void TickHold()
        {
            if (!_isHolding)
            {
                return;
            }

            _holdProgress = Mathf.Min(100f, _holdProgress + 6f);
            UpdateProgress();

            if (_holdProgress >= 100f)
            {
                CancelHold();
                Revealed?.Invoke();
            }
        }

        private void CancelHold()
        {
            _isHolding = false;
            _holdProgress = 0f;
            UpdateProgress();
            _holdTicker.Pause();
        }

        private void UpdateProgress()
        {
            _progressFill.style.width = new Length(_holdProgress, LengthUnit.Percent);
        }
    }
}
