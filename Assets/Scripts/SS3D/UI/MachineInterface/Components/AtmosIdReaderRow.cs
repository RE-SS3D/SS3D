using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AtmosIdReaderRow : VisualElement
    {
        public event Action ReadRequested;

        private readonly VisualElement _lockGlyph;
        private readonly Label _statusLine;
        private readonly Label _subLine;
        private readonly SteelButton _readButton;

        private string _idleSubline = "Read an ID to unlock preset modes and device control";
        private string _scanningSubline = "Checking access list";
        private string _grantedSubline = "Power and target pressure unlocked for this session";
        private string _deniedSubline = "ID not recognized by this unit";

        public AtmosIdReaderRow()
        {
            AddToClassList("atmos-id-reader-row");

            _lockGlyph = new VisualElement();
            _lockGlyph.AddToClassList("atmos-id-reader-row__lock");

            VisualElement lockShackle = new();
            lockShackle.AddToClassList("atmos-id-reader-row__lock-shackle");

            VisualElement lockBody = new();
            lockBody.AddToClassList("atmos-id-reader-row__lock-body");

            _lockGlyph.Add(lockShackle);
            _lockGlyph.Add(lockBody);

            _statusLine = new Label("No compatible ID read");
            _statusLine.AddToClassList("atmos-id-reader-row__status");
            _statusLine.AddToClassList("font-body");

            _subLine = new Label(_idleSubline);
            _subLine.AddToClassList("atmos-id-reader-row__sub");
            _subLine.AddToClassList("font-terminal");

            _readButton = new SteelButton { Text = "Read ID Card" };
            _readButton.AddToClassList("atmos-id-reader-row__button");
            _readButton.Clicked += () => ReadRequested?.Invoke();

            VisualElement copy = new();
            copy.AddToClassList("atmos-id-reader-row__copy");
            copy.Add(_statusLine);
            copy.Add(_subLine);

            VisualElement left = new();
            left.AddToClassList("atmos-id-reader-row__left");
            left.Add(_lockGlyph);
            left.Add(copy);

            Add(left);
            Add(_readButton);

            SetAccessState(scanning: false, granted: false, denied: false);
        }

        [UxmlAttribute]
        public string StatusText
        {
            get => _statusLine.text;
            set => _statusLine.text = value;
        }

        [UxmlAttribute]
        public string SubText
        {
            get => _subLine.text;
            set => _subLine.text = value;
        }

        [UxmlAttribute]
        public string ButtonText
        {
            get => _readButton.Text;
            set => _readButton.Text = value;
        }

        public void SetAccessState(bool scanning, bool granted, bool denied)
        {
            ClearStateClasses();

            if (scanning)
            {
                StatusText = "Reading ID…";
                SubText = _scanningSubline;
                ButtonText = "Reading…";
                _readButton.Disabled = true;
                _readButton.Variant = SteelButtonVariant.Steel;
                SetLockTone(StatusTone.Info);
                return;
            }

            if (granted)
            {
                StatusText = "Access Confirmed";
                SubText = _grantedSubline;
                ButtonText = "Lock Terminal";
                _readButton.Disabled = false;
                _readButton.Variant = SteelButtonVariant.Steel;
                SetLockTone(StatusTone.Success);
                SetStatusTone(StatusTone.Success);
                return;
            }

            if (denied)
            {
                StatusText = "Access Denied";
                SubText = _deniedSubline;
                ButtonText = "Read ID Card";
                _readButton.Disabled = false;
                _readButton.Variant = SteelButtonVariant.Danger;
                SetLockTone(StatusTone.Danger);
                SetStatusTone(StatusTone.Danger);
                return;
            }

            StatusText = "No compatible ID read";
            SubText = _idleSubline;
            ButtonText = "Read ID Card";
            _readButton.Disabled = false;
            _readButton.Variant = SteelButtonVariant.Steel;
            SetLockTone(StatusTone.Neutral);
        }

        public void SetIdleSubline(string subline) => _idleSubline = subline;

        public void SetGrantedSubline(string subline) => _grantedSubline = subline;

        public void SetDeniedSubline(string subline) => _deniedSubline = subline;

        private void ClearStateClasses()
        {
            _lockGlyph.RemoveFromClassList("tone-success");
            _lockGlyph.RemoveFromClassList("tone-neutral");
            _lockGlyph.RemoveFromClassList("tone-info");
            _lockGlyph.RemoveFromClassList("tone-danger");

            _statusLine.RemoveFromClassList("tone-success");
            _statusLine.RemoveFromClassList("tone-danger");
        }

        private void SetLockTone(StatusTone tone)
        {
            _lockGlyph.RemoveFromClassList("tone-success");
            _lockGlyph.RemoveFromClassList("tone-neutral");
            _lockGlyph.RemoveFromClassList("tone-info");
            _lockGlyph.RemoveFromClassList("tone-danger");

            string toneClass = tone switch
            {
                StatusTone.Success => "tone-success",
                StatusTone.Info => "tone-info",
                StatusTone.Danger => "tone-danger",
                _ => "tone-neutral",
            };

            _lockGlyph.AddToClassList(toneClass);
        }

        private void SetStatusTone(StatusTone tone)
        {
            string toneClass = tone switch
            {
                StatusTone.Success => "tone-success",
                StatusTone.Danger => "tone-danger",
                _ => null,
            };

            if (toneClass != null)
            {
                _statusLine.AddToClassList(toneClass);
            }
        }
    }
}
