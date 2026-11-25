using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>Small element builders shared by the lobby views (lobby effort Phase 4).</summary>
    internal static class LobbyUi
    {
        public static Label Text(string text, params string[] classes)
        {
            Label label = new(text) { enableRichText = false };
            foreach (string cls in classes) label.AddToClassList(cls);
            return label;
        }

        /// <summary>A Gurajada label; titling copy is upper case on the canvas.</summary>
        public static Label Titling(string text, params string[] classes)
        {
            Label label = Text(text.ToUpperInvariant(), classes);
            label.AddToClassList("lb-titling");
            return label;
        }

        public static Label Mono(string text, params string[] classes)
        {
            Label label = Text(text, classes);
            label.AddToClassList("lb-mono");
            return label;
        }

        public static VisualElement Box(params string[] classes)
        {
            VisualElement element = new();
            foreach (string cls in classes) element.AddToClassList(cls);
            return element;
        }

        public static VisualElement Icon(string modifier)
        {
            VisualElement icon = Box("lobby__icon", "lobby__icon--" + modifier);
            icon.pickingMode = PickingMode.Ignore;
            return icon;
        }

        /// <summary>
        /// A section's head (Lobby.uss, lobby__section-head): <paramref name="icon"/>'s glyph, the title in Disket and
        /// a hairline to the end of the line. <paramref name="classes"/> go on the row (a view's own margins).
        /// </summary>
        public static VisualElement SectionHead(string title, string icon, params string[] classes)
        {
            VisualElement head = Box("lobby__section-head");
            foreach (string cls in classes) head.AddToClassList(cls);
            VisualElement glyph = Icon(icon);
            glyph.AddToClassList("lobby__section-head-icon");
            head.Add(glyph);
            head.Add(Titling(title, "lobby__section-head-title"));
            VisualElement rule = Box("lobby__section-head-rule");
            rule.pickingMode = PickingMode.Ignore;
            head.Add(rule);
            return head;
        }

        /// <summary>A focused field masks keybinds (B = map editor, Escape = menu) while the player types.</summary>
        public static void HoldTextEntry(VisualElement field, InputTextEntryScope scope)
        {
            field.RegisterCallback<FocusInEvent>(_ => scope.Enter());
            field.RegisterCallback<FocusOutEvent>(_ => scope.Exit());
        }

        /// <summary>
        /// The lobby shell's text inputs, in the UI bible's look: the map editor's near-black search field. The default
        /// theme paints text inputs white and its class names vary across Unity 6 builds, so the fill is set inline on
        /// the inner input; Lobby.uss draws the edge and the focus line.
        /// </summary>
        public static void EditorInput(VisualElement field)
        {
            Color fill = new(0f, 0f, 0f, 0.9f);
            Color text = new(0.949f, 0.949f, 0.941f, 1f); // --ui-text

            void Apply()
            {
                VisualElement input = field.Q(className: "unity-base-text-field__input")
                    ?? field.Q(className: "unity-text-field__input")
                    ?? field.Q(className: "unity-base-field__input");
                if (input == null) return;
                input.style.backgroundColor = fill;
                input.style.color = text;
            }

            Apply();
            field.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }

        /// <summary>
        /// The character window's text inputs, in the UI bible's menu look: the character setup's translucent black
        /// field. Inline for the same reason as <see cref="EditorInput"/>; CharacterWindow.uss draws the edge.
        /// </summary>
        public static void MenuInput(VisualElement field)
        {
            Color fill = new(0f, 0f, 0f, 0.48f); // --ui-menu-field
            Color text = new(1f, 1f, 1f, 0.7f);

            void Apply()
            {
                VisualElement input = field.Q(className: "unity-base-text-field__input")
                    ?? field.Q(className: "unity-text-field__input")
                    ?? field.Q(className: "unity-base-field__input");
                if (input == null) return;
                input.style.backgroundColor = fill;
                input.style.color = text;
            }

            Apply();
            field.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }
    }
}
