using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DiagnosticsList : VisualElement
    {
        private readonly VisualElement _linesContainer;

        public DiagnosticsList()
        {
            AddToClassList("diagnostics-list");

            Label title = new("DIAGNOSTICS");
            title.AddToClassList("diagnostics-list__title");
            title.AddToClassList("font-arcade");

            _linesContainer = new VisualElement();
            _linesContainer.style.flexDirection = FlexDirection.Column;

            Add(title);
            Add(_linesContainer);
        }

        public void SetLines(IReadOnlyList<DiagnosticLine> lines)
        {
            _linesContainer.Clear();

            if (lines == null)
            {
                return;
            }

            foreach (DiagnosticLine line in lines)
            {
                VisualElement row = new();
                row.AddToClassList("diagnostics-list__line");

                Label glyph = new(line.Glyph);
                glyph.AddToClassList("diagnostics-list__glyph");
                glyph.AddToClassList("font-terminal");
                StatusToneUtility.ApplyTone(glyph, line.Tone);

                Label text = new(line.Text);
                text.AddToClassList("diagnostics-list__text");
                text.AddToClassList("font-terminal");
                StatusToneUtility.ApplyTone(text, line.Tone);

                row.Add(glyph);
                row.Add(text);
                _linesContainer.Add(row);
            }
        }
    }
}
