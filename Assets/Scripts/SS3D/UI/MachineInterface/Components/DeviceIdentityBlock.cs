using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DeviceIdentityBlock : VisualElement
    {
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly Label _metadata;

        public DeviceIdentityBlock()
        {
            AddToClassList("device-identity-block");

            _title = new Label("DEVICE");
            _title.AddToClassList("device-identity-block__title");
            _title.AddToClassList("font-titling");

            _subtitle = new Label();
            _subtitle.AddToClassList("device-identity-block__subtitle");

            _metadata = new Label();
            _metadata.AddToClassList("device-identity-block__metadata");
            _metadata.AddToClassList("font-terminal");
            _metadata.style.display = DisplayStyle.None;

            Add(_title);
            Add(_subtitle);
            Add(_metadata);
        }

        [UxmlAttribute]
        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        [UxmlAttribute]
        public string Subtitle
        {
            get => _subtitle.text;
            set => _subtitle.text = value;
        }

        [UxmlAttribute]
        public string Metadata
        {
            get => _metadata.text;
            set
            {
                _metadata.text = value;
                _metadata.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}
