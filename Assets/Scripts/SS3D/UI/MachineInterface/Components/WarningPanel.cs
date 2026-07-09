using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class WarningPanel : VisualElement
    {
        private readonly Label _title;
        private readonly VisualElement _linesRoot;

        public WarningPanel()
        {
            AddToClassList("warning-panel");

            _title = new Label("WARNING");
            _title.AddToClassList("warning-panel__title");
            _title.AddToClassList("font-titling");

            _linesRoot = new VisualElement();
            _linesRoot.AddToClassList("warning-panel__lines");

            Add(_title);
            Add(_linesRoot);
        }

        public void SetWarnings(IReadOnlyList<DiagnosticLine> warnings, string diagnosisHint)
        {
            _linesRoot.Clear();

            if (warnings == null || warnings.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            _title.text = string.IsNullOrEmpty(diagnosisHint)
                ? "WARNING"
                : $"Warning — {diagnosisHint}";

            foreach (DiagnosticLine warning in warnings)
            {
                VisualElement row = new();
                row.AddToClassList("warning-panel__line");

                Label glyph = new(warning.Glyph);
                glyph.AddToClassList("warning-panel__glyph");
                glyph.AddToClassList("tone-danger");

                Label text = new(warning.Text);
                text.AddToClassList("warning-panel__text");
                text.AddToClassList("font-body");

                row.Add(glyph);
                row.Add(text);
                _linesRoot.Add(row);
            }
        }
    }
}
