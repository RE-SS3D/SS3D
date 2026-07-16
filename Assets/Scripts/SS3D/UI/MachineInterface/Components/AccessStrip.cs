using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AccessStrip : VisualElement
    {
        public event Action LockRequested;

        private readonly Badge _badge;
        private readonly SteelButton _lockButton;

        public AccessStrip()
        {
            AddToClassList("access-strip");

            _badge = new Badge { Text = "Engineering — Authorized", Tone = StatusTone.Success };

            _lockButton = new SteelButton { Text = "Lock Terminal" };
            _lockButton.Clicked += () => LockRequested?.Invoke();

            Add(_badge);
            Add(_lockButton);
        }

        [UxmlAttribute]
        public string AuthorizedText
        {
            get => _badge.Text;
            set => _badge.Text = value;
        }
    }
}
