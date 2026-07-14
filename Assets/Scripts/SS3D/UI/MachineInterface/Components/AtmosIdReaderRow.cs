using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AtmosIdReaderRow : VisualElement
    {
        public event Action ReadRequested;

        private readonly VisualElement _lockGlyph;
        private readonly VisualElement _lockBody;
        private readonly Label _statusLine;
        private readonly Label _subLine;
        private readonly SteelButton _readButton;

        public AtmosIdReaderRow()
        {
            AddToClassList("atmos-id-reader-row");

            _lockGlyph = new VisualElement();
            _lockGlyph.AddToClassList("atmos-id-reader-row__lock");

            VisualElement lockShackle = new();
            lockShackle.AddToClassList("atmos-id-reader-row__lock-shackle");

            _lockBody = new VisualElement();
            _lockBody.AddToClassList("atmos-id-reader-row__lock-body");

            _lockGlyph.Add(lockShackle);
            _lockGlyph.Add(_lockBody);

            _statusLine = new Label("No compatible ID read");
            _statusLine.AddToClassList("atmos-id-reader-row__status");
            _statusLine.AddToClassList("font-body");

            _subLine = new Label("Read an ID to unlock preset modes and device control");
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

            SetAccessState(locked: true, scanning: false);
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

        public void SetAccessState(bool locked, bool scanning, bool granted = false)
        {
            if (scanning)
            {
                StatusText = "Reading ID…";
                SubText = string.Empty;
                ButtonText = "Reading…";
                _readButton.Disabled = true;
                SetLockTone(StatusTone.Info);
                return;
            }

            if (granted)
            {
                StatusText = "Access Confirmed";
                ButtonText = "Lock Terminal";
                _readButton.Disabled = false;
                SetLockTone(StatusTone.Success);
                return;
            }

            if (locked)
            {
                StatusText = "No compatible ID read";
            }

            ButtonText = "Read ID Card";
            _readButton.Disabled = false;
            SetLockTone(StatusTone.Neutral);
        }

        private void SetLockTone(StatusTone tone)
        {
            _lockGlyph.RemoveFromClassList("tone-success");
            _lockGlyph.RemoveFromClassList("tone-neutral");
            _lockGlyph.RemoveFromClassList("tone-info");

            string toneClass = tone switch
            {
                StatusTone.Success => "tone-success",
                StatusTone.Info => "tone-info",
                _ => "tone-neutral",
            };

            _lockGlyph.AddToClassList(toneClass);
        }
    }
}
