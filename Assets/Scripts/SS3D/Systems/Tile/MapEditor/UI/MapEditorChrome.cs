using System;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// Small pieces of the map editor's chrome (UI bible): a tool cell's hotkey mark, its hover hint (a floating plate
    /// whose caret points back at the cell, with key names as chips), the hint bar's text, and a popover's close button.
    /// </summary>
    public static class MapEditorChrome
    {
        /// <summary>
        /// Key names in a hint, as the view model writes them ("Shift+drag", "R to rotate", "V / Shift+V"): drawn as
        /// chips. Single letters count only as standalone capitals.
        /// </summary>
        private static readonly Regex KeyWords = new(@"(?<!\w)(Shift|Ctrl|Cmd|Alt|Esc|R|V)(?!\w)");

        /// <summary>The tool's hotkey in the cell's corner, like a hand's L / R mark.</summary>
        public static void AddHotkeyMark(Button button, string key)
        {
            Label mark = new(key) { pickingMode = PickingMode.Ignore, enableRichText = false };
            mark.AddToClassList("map-editor-icon-btn__key");
            button.Add(mark);
        }

        /// <summary>
        /// The button in a wrapper with its hover hint: its name and keys on a floating plate beside or under it, the
        /// caret pointing back at the button. Shown on MouseEnter, hidden on MouseLeave.
        /// </summary>
        public static VisualElement WithHint(Button button, string label, MapEditorPopoverSide side, params string[] keys)
        {
            VisualElement wrap = new() { pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("map-editor-toolbar-btn-wrap");

            VisualElement hint = new() { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("map-editor-toolbar-hint");
            hint.AddToClassList(side switch
            {
                MapEditorPopoverSide.Right => "map-editor-toolbar-hint--opens-right",
                MapEditorPopoverSide.Left => "map-editor-toolbar-hint--opens-left",
                _ => "map-editor-toolbar-hint--opens-down",
            });
            hint.style.display = DisplayStyle.None;
            hint.Add(MapEditorPopoverPlacement.CreateCaret(side));

            Label text = new(label) { pickingMode = PickingMode.Ignore, enableRichText = false };
            text.AddToClassList("map-editor-toolbar-hint__label");
            hint.Add(text);
            foreach (string key in keys)
                hint.Add(Key(key));

            button.RegisterCallback<MouseEnterEvent>(_ => hint.style.display = DisplayStyle.Flex);
            button.RegisterCallback<MouseLeaveEvent>(_ => hint.style.display = DisplayStyle.None);
            wrap.Add(button);
            wrap.Add(hint);
            return wrap;
        }

        /// <summary>A key name: Disket capitals on a dark chip.</summary>
        public static Label Key(string key)
        {
            Label chip = new(key.ToUpperInvariant()) { pickingMode = PickingMode.Ignore, enableRichText = false };
            chip.AddToClassList("map-editor-key");
            return chip;
        }

        /// <summary>
        /// Fills the hint bar: the hint's words in lower case, " — " as " · ", and key names as chips ("Shift+drag" and
        /// "Shift-drag" read [SHIFT] drag).
        /// </summary>
        public static void FillHint(VisualElement bar, string hint)
        {
            bar.Clear();
            string text = (hint ?? string.Empty).Replace(" — ", " · ");
            int at = 0;
            foreach (Match match in KeyWords.Matches(text))
            {
                if (match.Index < at)
                    continue;

                AddHintText(bar, text.Substring(at, match.Index - at));
                bar.Add(Key(match.Value));
                at = match.Index + match.Length;
                // "Shift+drag", "Shift-drag": the chip stands for the joiner.
                if (at < text.Length && (text[at] == '+' || text[at] == '-'))
                {
                    at++;
                    AddHintText(bar, " ");
                }
            }

            AddHintText(bar, text.Substring(at));
        }

        /// <summary>A popover's close button: a bare × in a cell-shaped hit area.</summary>
        public static Button CloseButton(Action onClose)
        {
            Button close = new(onClose) { name = "popover-close" };
            close.AddToClassList("map-editor-popover-close");
            Label glyph = new("×") { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("map-editor-popover-close__glyph");
            close.Add(glyph);
            return close;
        }

        private static void AddHintText(VisualElement bar, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Label label = new(text.ToLowerInvariant()) { pickingMode = PickingMode.Ignore, enableRichText = false };
            label.AddToClassList("map-editor-hint__text");
            bar.Add(label);
        }
    }
}
