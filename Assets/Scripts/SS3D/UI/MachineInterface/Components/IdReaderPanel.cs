using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class IdReaderPanel : VisualElement
    {
        public event Action ReadIdRequested;

        private readonly Label _statusLine;
        private readonly Label _subLine;
        private readonly SteelButton _readButton;

        public IdReaderPanel()
        {
            AddToClassList("id-reader-panel");

            _statusLine = new Label("No compatible ID read");
            _statusLine.AddToClassList("id-reader-panel__status");
            _statusLine.AddToClassList("font-body");

            _subLine = new Label("Read a Medical-tier ID to unlock gated items");
            _subLine.AddToClassList("id-reader-panel__sub");
            _subLine.AddToClassList("font-terminal");

            _readButton = new SteelButton { Text = "Read ID Card", Disabled = true };
            _readButton.Clicked += () => ReadIdRequested?.Invoke();

            VisualElement body = new();
            body.AddToClassList("id-reader-panel__body");
            body.Add(_statusLine);
            body.Add(_subLine);
            body.Add(_readButton);

            Add(body);
        }

        public void SetState(bool idScanned, bool scanning)
        {
            if (idScanned)
            {
                _statusLine.text = "Medical Access — Confirmed";
                _subLine.text = "Gated items unlocked for this session";
                _readButton.Text = "ID Confirmed";
                _readButton.Disabled = true;
            }
            else if (scanning)
            {
                _statusLine.text = "Reading ID card…";
                _subLine.text = string.Empty;
                _readButton.Text = "Reading…";
                _readButton.Disabled = true;
            }
            else
            {
                _statusLine.text = "No compatible ID read";
                _subLine.text = "Read a Medical-tier ID to unlock gated items";
                _readButton.Text = "Read ID Card";
                _readButton.Disabled = true;
            }
        }
    }
}
