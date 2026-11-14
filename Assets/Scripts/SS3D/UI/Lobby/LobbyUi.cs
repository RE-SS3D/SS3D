using SS3D.Systems.Inputs;
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

        /// <summary>A focused field masks keybinds (B = map editor, Escape = menu) while the player types.</summary>
        public static void HoldTextEntry(VisualElement field, InputTextEntryScope scope)
        {
            field.RegisterCallback<FocusInEvent>(_ => scope.Enter());
            field.RegisterCallback<FocusOutEvent>(_ => scope.Exit());
        }

        /// <summary>
        /// The default theme paints text inputs white and its class names vary across Unity 6 builds, so the inset
        /// fill is also set inline on the inner input.
        /// </summary>
        public static void DarkInput(VisualElement field)
        {
            Color fill = new(0.1647059f, 0.1647059f, 0.1803922f, 1f); // --lb-inset #2a2a2e

            void Apply()
            {
                VisualElement input = field.Q(className: "unity-base-text-field__input")
                    ?? field.Q(className: "unity-text-field__input")
                    ?? field.Q(className: "unity-base-field__input");
                if (input == null) return;
                input.style.backgroundColor = fill;
                input.style.color = Color.white;
            }

            Apply();
            field.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }
    }
}
