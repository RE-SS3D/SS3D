using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class PanelSection : VisualElement
    {
        private readonly Label _header;
        private readonly VisualElement _content;

        public PanelSection()
        {
            AddToClassList("panel-section");

            _header = new Label("SECTION");
            _header.AddToClassList("panel-section__header");
            _header.AddToClassList("font-titling");

            _content = new VisualElement();
            _content.AddToClassList("panel-section__content");

            Add(_header);
            Add(_content);
        }

        public override VisualElement contentContainer => _content;

        [UxmlAttribute]
        public string Title
        {
            get => _header.text;
            set => _header.text = value;
        }
    }
}
