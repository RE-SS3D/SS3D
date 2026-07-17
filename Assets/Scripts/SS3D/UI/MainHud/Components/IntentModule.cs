using System;
using SS3D.Interactions;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-right help/harm segmented toggle plus the fading modifier-hint row (Shift/swap, Ctrl/disarm,
    /// Alt/grab). The hints are display-only here - chorded modifier click resolution (§7 of the design doc)
    /// is a separate, not-yet-built interaction-layer feature.
    /// </summary>
    [UxmlElement]
    public partial class IntentModule : VisualElement
    {
        public event Action ToggleRequested;

        private readonly VisualElement _helpSegment;
        private readonly VisualElement _harmSegment;

        public IntentModule()
        {
            AddToClassList("intent-module");

            VisualElement hints = new();
            hints.AddToClassList("intent-module__hints");
            hints.Add(BuildHint("Shift", "swap"));
            hints.Add(BuildHint("Ctrl", "disarm"));
            hints.Add(BuildHint("Alt", "grab"));

            VisualElement toggle = new();
            toggle.AddToClassList("intent-toggle");

            _helpSegment = new Label("Help");
            _helpSegment.AddToClassList("intent-toggle__segment");
            _helpSegment.AddToClassList("intent-toggle__segment--help");
            _helpSegment.AddToClassList("font-titling");

            _harmSegment = new Label("Harm");
            _harmSegment.AddToClassList("intent-toggle__segment");
            _harmSegment.AddToClassList("intent-toggle__segment--harm");
            _harmSegment.AddToClassList("font-titling");

            toggle.Add(_helpSegment);
            toggle.Add(_harmSegment);
            toggle.RegisterCallback<ClickEvent>(_ => ToggleRequested?.Invoke());

            Add(hints);
            Add(toggle);

            SetIntent(IntentType.Help);
        }

        public void SetIntent(IntentType intent)
        {
            bool isHelp = intent != IntentType.Harm;
            _helpSegment.EnableInClassList("intent-toggle__segment--active", isHelp);
            _harmSegment.EnableInClassList("intent-toggle__segment--active", !isHelp);
        }

        private static VisualElement BuildHint(string key, string label)
        {
            VisualElement hint = new();
            hint.AddToClassList("intent-hint");

            Label keyLabel = new(key);
            keyLabel.AddToClassList("intent-hint__key");
            keyLabel.AddToClassList("font-terminal");

            Label textLabel = new(label);
            textLabel.AddToClassList("intent-hint__label");
            textLabel.AddToClassList("font-body");

            hint.Add(keyLabel);
            hint.Add(textLabel);
            return hint;
        }
    }
}
