using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class SmesPowerFlowRow : VisualElement
    {
        private readonly Label _inputGlyph;
        private readonly Label _outputGlyph;

        public SmesPowerFlowRow()
        {
            AddToClassList("smes-power-flow-row");

            Label stationGrid = CreateNode("STATION GRID", "smes-power-flow-row__node");
            _inputGlyph = CreateGlyph();
            Label smesNode = CreateNode("SMES", "smes-power-flow-row__node smes-power-flow-row__node--core");
            smesNode.RemoveFromClassList("font-terminal");
            smesNode.AddToClassList("font-titling");
            _outputGlyph = CreateGlyph();
            Label distributionGrid = CreateNode("DISTRIBUTION", "smes-power-flow-row__node");

            Add(stationGrid);
            Add(_inputGlyph);
            Add(smesNode);
            Add(_outputGlyph);
            Add(distributionGrid);
        }

        public void SetFlow(bool inputActive, bool outputActive, StatusTone inputTone, StatusTone outputTone)
        {
            SetGlyph(_inputGlyph, inputActive, inputTone);
            SetGlyph(_outputGlyph, outputActive, outputTone);
        }

        private static Label CreateNode(string text, string className)
        {
            Label label = new(text);
            label.AddToClassList(className);
            label.AddToClassList("font-terminal");
            return label;
        }

        private static Label CreateGlyph()
        {
            Label glyph = new("→");
            glyph.AddToClassList("smes-power-flow-row__glyph");
            glyph.AddToClassList("font-arcade");
            return glyph;
        }

        private static void SetGlyph(Label glyph, bool active, StatusTone tone)
        {
            if (active)
            {
                glyph.text = "→";
            }
            else if (tone == StatusTone.Danger)
            {
                glyph.text = "✕";
            }
            else
            {
                glyph.text = "·";
            }

            glyph.RemoveFromClassList("tone-success");
            glyph.RemoveFromClassList("tone-warning");
            glyph.RemoveFromClassList("tone-danger");
            glyph.RemoveFromClassList("tone-info");

            if (!active && tone == StatusTone.Danger)
            {
                glyph.AddToClassList("tone-danger");
            }
            else if (active)
            {
                string toneClass = tone switch
                {
                    StatusTone.Warning => "tone-warning",
                    StatusTone.Danger => "tone-danger",
                    _ => "tone-success",
                };
                glyph.AddToClassList(toneClass);
            }
            else
            {
                glyph.AddToClassList("tone-info");
            }
        }
    }
}
