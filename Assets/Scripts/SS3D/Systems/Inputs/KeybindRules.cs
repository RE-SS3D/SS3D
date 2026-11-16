using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    public sealed class KeybindRow
    {
        public KeybindRow(string id, string label, string group, string path, string modifierPath, int contextMask, bool readOnly,
            string defaultPath = null, string hint = null, string fixedText = null)
        {
            Id = id;
            Label = label;
            Group = group;
            Path = path;
            ModifierPath = modifierPath;
            ContextMask = contextMask;
            ReadOnly = readOnly;
            DefaultPath = defaultPath ?? path;
            Hint = hint;
            FixedText = fixedText;
        }

        public string Id { get; }
        public string Label { get; }
        public string Group { get; }
        public string Path { get; }
        public string ModifierPath { get; }
        public int ContextMask { get; }
        public bool ReadOnly { get; }

        /// <summary>The key before any override; the Controls tab marks rows that differ.</summary>
        public string DefaultPath { get; }

        /// <summary>A line under the label ("Hold"), or null.</summary>
        public string Hint { get; }

        /// <summary>What a read-only row shows instead of its key ("Hold Shift"), or null for the key's name.</summary>
        public string FixedText { get; }
    }

    public readonly struct KeybindChange
    {
        public KeybindChange(string rowId, string path)
        {
            RowId = rowId;
            Path = path;
        }

        public string RowId { get; }
        public string Path { get; }

        public override string ToString() => $"{RowId} -> {Path}";
    }

    public sealed class KeybindResult
    {
        public bool Accepted;
        public KeybindChange[] Changes = new KeybindChange[0];
        public string Notice;
    }

    /// <summary>
    /// Player rebinding rules (2026-10_player-settings Decisions 10–11), pure over a binding table. A chord is the
    /// fixed modifier plus the key; two rows conflict when they share a chord and some input context enables both.
    /// A conflict swaps: every row holding the new chord takes the rebound row's old key. Read-only rows are never
    /// moved, so taking their chord is refused.
    /// </summary>
    public static class KeybindRules
    {
        public static KeybindResult Rebind(IReadOnlyList<KeybindRow> rows, string rowId, string newPath)
        {
            KeybindRow target = rows.FirstOrDefault(r => r.Id == rowId);
            if (target == null || target.ReadOnly) return new KeybindResult();
            if (SamePath(target.Path, newPath)) return new KeybindResult { Accepted = true };

            KeybindRow[] conflicts = rows.Where(r => r != target && (r.ContextMask & target.ContextMask) != 0
                && SamePath(r.ModifierPath, target.ModifierPath) && SamePath(r.Path, newPath)).ToArray();

            KeybindRow reserved = conflicts.FirstOrDefault(r => r.ReadOnly);
            if (reserved != null)
                return new KeybindResult { Notice = $"{DisplayName(newPath, target.ModifierPath)} is reserved for {reserved.Label}" };

            List<KeybindChange> changes = new() { new KeybindChange(target.Id, newPath) };
            changes.AddRange(conflicts.Select(r => new KeybindChange(r.Id, target.Path)));
            string notice = conflicts.Length == 0 ? null
                : $"{JoinLabels(conflicts)} {(conflicts.Length == 1 ? "is" : "are")} now {DisplayName(target.Path, target.ModifierPath)}";
            return new KeybindResult { Accepted = true, Changes = changes.ToArray(), Notice = notice };
        }

        /// <summary>"Ctrl + Q", "Caps Lock", "Forward": the binding as the Controls tab shows it.</summary>
        public static string DisplayName(string path, string modifierPath)
        {
            string key = Readable(path);
            return string.IsNullOrEmpty(modifierPath) ? key : $"{Readable(modifierPath)} + {key}";
        }

        private static string Readable(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Unbound";
            string text = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            // The Input System says "Control"; the HUD's modifier hints (main-hud.md §7) say Ctrl.
            return text.Replace("Control", "Ctrl");
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);

        private static string JoinLabels(IReadOnlyList<KeybindRow> rows) => rows.Count == 1 ? rows[0].Label
            : string.Join(", ", rows.Take(rows.Count - 1).Select(r => r.Label)) + " and " + rows[rows.Count - 1].Label;
    }
}
