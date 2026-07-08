using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class VerticalPowerFlow : VisualElement
    {
        private readonly Label _inputGlyph;
        private readonly Label _outputGlyph;

        public VerticalPowerFlow()
        {
            AddToClassList("vertical-power-flow");

            Label stationGrid = CreateNode("STATION GRID", "vertical-power-flow__node");
            _inputGlyph = CreateGlyph();
            Label smesNode = CreateNode("SMES", "vertical-power-flow__node vertical-power-flow__node--core");
            _outputGlyph = CreateGlyph();
            Label distributionGrid = CreateNode("DISTRIBUTION GRID", "vertical-power-flow__node");

            Add(stationGrid);
            Add(_inputGlyph);
            Add(smesNode);
            Add(_outputGlyph);
            Add(distributionGrid);
        }

        public void SetFlow(bool inputActive, bool outputActive, StatusTone inputTone, StatusTone outputTone)
        {
            SetGlyph(_inputGlyph, inputActive, inputTone, inactiveGlyph: "·", brokenGlyph: "✕");
            SetGlyph(_outputGlyph, outputActive, outputTone, inactiveGlyph: "·", brokenGlyph: "✕");
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
            Label glyph = new("↓");
            glyph.AddToClassList("vertical-power-flow__glyph");
            return glyph;
        }

        private static void SetGlyph(Label glyph, bool active, StatusTone tone, string inactiveGlyph, string brokenGlyph)
        {
            if (active)
            {
                glyph.text = "↓";
            }
            else if (tone == StatusTone.Danger)
            {
                glyph.text = brokenGlyph;
            }
            else
            {
                glyph.text = inactiveGlyph;
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
