using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class WarningPanel : VisualElement
    {
        private readonly Label _title;
        private readonly VisualElement _linesRoot;
        private readonly Label _hint;

        public WarningPanel()
        {
            AddToClassList("warning-panel");
            AddToClassList("tone-bg-danger");

            _title = new Label("WARNING");
            _title.AddToClassList("warning-panel__title");
            _title.AddToClassList("font-titling");
            _title.AddToClassList("tone-danger");

            _linesRoot = new VisualElement();
            _linesRoot.AddToClassList("warning-panel__lines");

            _hint = new Label();
            _hint.AddToClassList("warning-panel__hint");
            _hint.AddToClassList("font-body");

            Add(_title);
            Add(_linesRoot);
            Add(_hint);
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

            _hint.text = diagnosisHint ?? string.Empty;
            _hint.style.display = string.IsNullOrEmpty(diagnosisHint) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
