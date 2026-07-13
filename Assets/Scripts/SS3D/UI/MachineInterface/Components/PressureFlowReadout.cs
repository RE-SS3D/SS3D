using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class PressureFlowReadout : VisualElement
    {
        private readonly Label _externalLabel;
        private readonly Label _externalValue;
        private readonly Label _glyph;
        private readonly Label _internalLabel;
        private readonly Label _internalValue;

        public PressureFlowReadout()
        {
            AddToClassList("pressure-flow-readout");

            VisualElement external = CreateNode("Room (external)", out _externalLabel, out _externalValue, core: false);
            _glyph = new Label("·");
            _glyph.AddToClassList("pressure-flow-readout__glyph");
            _glyph.AddToClassList("font-arcade");
            VisualElement internalNode = CreateNode("Pipe network (internal)", out _internalLabel, out _internalValue, core: true);

            Add(external);
            Add(_glyph);
            Add(internalNode);
        }

        [UxmlAttribute]
        public string ExternalLabel
        {
            get => _externalLabel.text;
            set => _externalLabel.text = value;
        }

        [UxmlAttribute]
        public string ExternalValue
        {
            get => _externalValue.text;
            set => _externalValue.text = value;
        }

        [UxmlAttribute]
        public string InternalLabel
        {
            get => _internalLabel.text;
            set => _internalLabel.text = value;
        }

        [UxmlAttribute]
        public string InternalValue
        {
            get => _internalValue.text;
            set => _internalValue.text = value;
        }

        [UxmlAttribute]
        public string FlowGlyph
        {
            get => _glyph.text;
            set => _glyph.text = value;
        }

        [UxmlAttribute]
        public StatusTone ExternalTone
        {
            get => StatusTone.Info;
            set => StatusToneUtility.ApplyTone(_externalValue, value);
        }

        [UxmlAttribute]
        public StatusTone FlowTone
        {
            get => StatusTone.Info;
            set => StatusToneUtility.ApplyTone(_glyph, value);
        }

        private static VisualElement CreateNode(string title, out Label titleLabel, out Label valueLabel, bool core)
        {
            VisualElement node = new();
            node.AddToClassList(core ? "pressure-flow-readout__node pressure-flow-readout__node--core" : "pressure-flow-readout__node");

            titleLabel = new Label(title);
            titleLabel.AddToClassList("pressure-flow-readout__node-title");
            titleLabel.AddToClassList("font-arcade");

            valueLabel = new Label("0.0 kPa");
            valueLabel.AddToClassList("pressure-flow-readout__node-value");
            valueLabel.AddToClassList("font-terminal");

            node.Add(titleLabel);
            node.Add(valueLabel);
            return node;
        }
    }
}
