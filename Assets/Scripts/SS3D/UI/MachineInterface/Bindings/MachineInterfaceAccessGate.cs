using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    /// <summary>
    /// Client-side session gate for engineering terminal controls (ID swipe / lock).
    /// </summary>
    public sealed class MachineInterfaceAccessGate
    {
        public event Action StateChanged;

        private readonly VisualElement _lockedPanel;
        private readonly VisualElement _unlockedPanel;
        private readonly AccessGatePanel _gatePanel;
        private bool _accessGranted;
        private bool _scanning;
        private IVisualElementScheduledItem _scanSchedule;

        public MachineInterfaceAccessGate(
            VisualElement lockedPanel,
            VisualElement unlockedPanel,
            AccessGatePanel gatePanel)
        {
            _lockedPanel = lockedPanel;
            _unlockedPanel = unlockedPanel;
            _gatePanel = gatePanel;

            if (_gatePanel != null)
            {
                _gatePanel.SwipeRequested += HandleSwipeRequested;
            }
        }

        public bool AccessGranted => _accessGranted;

        public void Reset()
        {
            _scanSchedule?.Pause();
            _scanSchedule = null;
            _accessGranted = false;
            _scanning = false;
            ApplyVisibility();
        }

        public void Lock()
        {
            Reset();
        }

        public void Disconnect()
        {
            _scanSchedule?.Pause();
            _scanSchedule = null;

            if (_gatePanel != null)
            {
                _gatePanel.SwipeRequested -= HandleSwipeRequested;
            }
        }

        private void HandleSwipeRequested()
        {
            if (_accessGranted || _scanning || _gatePanel == null)
            {
                return;
            }

            _scanning = true;
            _gatePanel.SetState(scanning: true);
            _scanSchedule = _gatePanel.schedule.Execute(() =>
            {
                _scanning = false;
                _accessGranted = true;
                ApplyVisibility();
            }).StartingIn(800);
        }

        private void ApplyVisibility()
        {
            if (_lockedPanel != null)
            {
                _lockedPanel.style.display = _accessGranted ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_unlockedPanel != null)
            {
                _unlockedPanel.style.display = _accessGranted ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!_accessGranted && !_scanning && _gatePanel != null)
            {
                _gatePanel.SetState(scanning: false);
            }

            StateChanged?.Invoke();
        }
    }
}
