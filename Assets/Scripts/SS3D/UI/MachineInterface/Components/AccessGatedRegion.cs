using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// Hosts an ID gate and a named controls subtree declared in UXML.
    /// </summary>
    [UxmlElement]
    public partial class AccessGatedRegion : VisualElement
    {
        public const string ControlsHiddenClass = "access-gated-region__controls--hidden";

        private AccessGatePanel _gate;
        private VisualElement _controls;
        private bool _accessGranted;
        private bool _scanning;
        private bool _serverDriven;
        private IVisualElementScheduledItem _scanSchedule;
        private bool _wired;

        public AccessGatedRegion()
        {
            AddToClassList("access-gated-region");
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        }

        public AccessGatePanel Gate => _gate;

        public VisualElement Controls => _controls;

        public bool AccessGranted => _accessGranted;

        public void Initialize(bool serverDriven = false)
        {
            _serverDriven = serverDriven;
            EnsureWired();
        }

        public void Lock()
        {
            EnsureWired();
            _scanSchedule?.Pause();
            _scanSchedule = null;
            _accessGranted = false;
            _scanning = false;
            ShowGate();
        }

        public void ApplyAccessState(bool granted, bool scanning, bool denied = false)
        {
            EnsureWired();
            _scanSchedule?.Pause();
            _scanSchedule = null;
            _accessGranted = granted;
            _scanning = scanning;

            if (_accessGranted)
            {
                if (_gate != null)
                {
                    _gate.style.display = DisplayStyle.None;
                    _gate.SetState(scanning: false);
                }

                if (_controls != null)
                {
                    _controls.style.display = DisplayStyle.Flex;
                    _controls.RemoveFromClassList(ControlsHiddenClass);
                }

                return;
            }

            ShowGate();
            _gate?.SetState(scanning, denied);
        }

        private void OnAttachedToPanel(AttachToPanelEvent _)
        {
            EnsureWired();
        }

        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            _gate = this.Q<AccessGatePanel>("access-gate");
            _controls = this.Q<VisualElement>("access-controls");

            if (_gate == null || _controls == null)
            {
                return;
            }

            _wired = true;
            _gate.SwipeRequested += HandleSwipeRequested;
            ShowGate();
        }

        private void HandleSwipeRequested()
        {
            EnsureWired();

            if (_serverDriven || _accessGranted || _scanning || _gate == null)
            {
                return;
            }

            _scanning = true;
            _gate.SetState(scanning: true);

            _scanSchedule?.Pause();
            _scanSchedule = schedule.Execute(GrantAccess).StartingIn(800);
        }

        private void GrantAccess()
        {
            _scanSchedule = null;
            _scanning = false;
            _accessGranted = true;

            _gate.style.display = DisplayStyle.None;
            _gate.SetState(scanning: false);

            _controls.style.display = DisplayStyle.Flex;
            _controls.RemoveFromClassList(ControlsHiddenClass);
        }

        private void ShowGate()
        {
            if (_gate == null || _controls == null)
            {
                return;
            }

            _gate.style.display = DisplayStyle.Flex;
            _gate.SetState(scanning: false, denied: false);

            _controls.style.display = DisplayStyle.None;
            _controls.AddToClassList(ControlsHiddenClass);
        }
    }
}
