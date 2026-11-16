using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DeviceFooter : VisualElement
    {
        private readonly Label _label;

        public DeviceFooter()
        {
            AddToClassList("device-footer");

            _label = new Label();
            _label.AddToClassList("device-footer__label");
            _label.AddToClassList("font-terminal");
            Add(_label);
        }

        [UxmlAttribute]
        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }
    }
}
