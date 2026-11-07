using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Player keybinds (2026-10_player-settings Phase 3): the Controls tab's rows, rebinding through
    /// <see cref="KeybindRules"/>, override save/load and key capture. Rows come from a fixed list of the actions a
    /// player may see (Decisions 9–10); every other live binding is a hidden read-only row, so it is reserved.
    /// </summary>
    public sealed partial class InputSubSystem
    {
        /// <summary>Where a row's key lives: an action binding, or nothing for a chord-only row (Ctrl/Alt + click).</summary>
        private readonly struct RowTarget
        {
            public readonly InputAction Action;
            public readonly int KeyIndex;
            public readonly int[] ExtraIndices;

            public RowTarget(InputAction action, int keyIndex, int[] extraIndices)
            {
                Action = action;
                KeyIndex = keyIndex;
                ExtraIndices = extraIndices;
            }
        }

        private readonly Dictionary<string, RowTarget> _rowTargets = new();

        /// <summary>Rows in Controls-tab order. A null <see cref="KeybindRow.Group"/> marks a hidden, reserved binding.</summary>
        public IReadOnlyList<KeybindRow> BuildKeybindRows()
        {
            _rowTargets.Clear();
            List<KeybindRow> rows = new();
            HashSet<InputAction> shown = new();

            void Part(string group, InputAction action, string part, string label, bool readOnly = false)
            {
                int index = action.bindings.IndexOf(b => b.isPartOfComposite && b.name == part);
                if (index >= 0) AddRow(rows, group, label, action, index, null, readOnly);
                shown.Add(action);
            }

            void Chord(string group, InputAction action, string composite, string label)
            {
                int start = action.bindings.IndexOf(b => b.isComposite && (composite == null || b.name == composite));
                if (start < 0) return;
                int modifier = start + 1 + action.bindings.Skip(start + 1).TakeWhile(b => b.isPartOfComposite).ToList().FindIndex(b => b.name == "modifier");
                int key = start + 1 + action.bindings.Skip(start + 1).TakeWhile(b => b.isPartOfComposite).ToList().FindIndex(b => b.name == "binding");
                AddRow(rows, group, label, action, key, action.bindings[modifier].effectivePath, false);
                shown.Add(action);
            }

            void Plain(string group, InputAction action, string label, bool readOnly = false)
            {
                int index = action.bindings.IndexOf(b => !b.isComposite && !b.isPartOfComposite);
                if (index >= 0) AddRow(rows, group, label, action, index, null, readOnly);
                shown.Add(action);
            }

            Controls.MovementActions movement = Inputs.Movement;
            Part("Movement", movement.Movement, "up", "Move Forward");
            Part("Movement", movement.Movement, "down", "Move Back");
            Part("Movement", movement.Movement, "left", "Move Left");
            Part("Movement", movement.Movement, "right", "Move Right");
            Plain("Movement", movement.ToggleRun, "Toggle Run");

            Controls.CameraActions camera = Inputs.Camera;
            Chord("Camera", camera.HorizontalRotation, "Left", "Rotate Left");
            Chord("Camera", camera.HorizontalRotation, "Right", "Rotate Right");
            Chord("Camera", camera.SnapLeft, null, "Snap Left");
            Chord("Camera", camera.SnapRight, null, "Snap Right");
            Part("Camera", camera.VerticalRotation, "negative", "Tilt (−)");
            Part("Camera", camera.VerticalRotation, "positive", "Tilt (+)");
            AddFixedRow(rows, "Camera", "Mouse Rotation (hold + drag)", "<Mouse>/middleButton", MaskOf(camera.MouseRotation));
            shown.Add(camera.MouseRotation);
            AddFixedRow(rows, "Camera", "Zoom", "<Mouse>/scroll", MaskOf(camera.Zoom));
            shown.Add(camera.Zoom);

            Controls.HotkeysActions hotkeys = Inputs.Hotkeys;
            Plain("Hotkeys", hotkeys.SwapHands, "Swap Hands");
            Plain("Hotkeys", hotkeys.Use, "Use");
            Plain("Hotkeys", hotkeys.Drop, "Drop");
            Plain("Hotkeys", hotkeys.ToggleInternals, "Toggle Internals");
            Plain("Hotkeys", hotkeys.ToggleMagboots, "Toggle Magboots");

            Controls.InteractionsActions interactions = Inputs.Interactions;
            Plain("Interactions", interactions.RunPrimary, "Interact", readOnly: true);
            Plain("Interactions", interactions.ViewInteractions, "Interaction Menu", readOnly: true);
            Plain("Interactions", interactions.CancelInteraction, "Cancel Interaction");

            Plain("General", _toggleIntent, "Toggle Help / Harm");
            Plain("General", _openLocalSpeechCompose, "Local Chat");
            Plain("General", _detailedExamine, "Examine (hold)", readOnly: true);
            int gameplay = MaskOf(_toggleIntent);
            AddFixedRow(rows, "General", "Disarm (hold + click)", "<Keyboard>/ctrl", gameplay);
            AddFixedRow(rows, "General", "Grab (hold + click)", "<Keyboard>/alt", gameplay);
            Plain("General", Inputs.Other.ToggleMenu, "Menu", readOnly: true);
            Plain("General", _openBugReport, "Report a Bug");
            Plain("General", Inputs.Other.PrimaryClick, "Primary Click", readOnly: true);
            Plain("General", Inputs.Other.SecondaryClick, "Secondary Click", readOnly: true);

            Controls.TileCreatorActions tile = Inputs.TileCreator;
            Plain("Map Editor", tile.ToggleMenu, "Build Menu");
            Plain("Map Editor", tile.Rotate, "Rotate");
            Plain("Map Editor", tile.CycleVariant, "Next Variant (Shift: previous)");
            Plain("Map Editor", tile.Replace, "Replace (hold)");
            Plain("Map Editor", tile.SquareDrag, "Square Drag (hold)");
            Plain("Map Editor", tile.Place, "Place", readOnly: true);
            Plain("Map Editor", tile.Delete, "Delete", readOnly: true);

            // Everything else that still has a binding (console, debug toggles, hacks) is hidden but reserved.
            foreach (InputAction action in CollectAllActions().Where(a => !shown.Contains(a)))
                for (int i = 0; i < action.bindings.Count; i++)
                    if (!action.bindings[i].isComposite && !string.IsNullOrEmpty(action.bindings[i].effectivePath))
                        AddRow(rows, null, action.name, action, i, null, true);

            return rows;
        }

        /// <summary>Applies <see cref="KeybindRules.Rebind"/> to the live actions; returns the result for the notice.</summary>
        public KeybindResult Rebind(string rowId, string path)
        {
            KeybindResult result = KeybindRules.Rebind(BuildKeybindRows(), rowId, path);
            foreach (KeybindChange change in result.Changes)
            {
                if (!_rowTargets.TryGetValue(change.RowId, out RowTarget target) || target.Action == null) continue;
                target.Action.ApplyBindingOverride(target.KeyIndex, change.Path);
                // One key per action (Decision 11): extra plain bindings (right Shift beside left Shift) go unbound.
                foreach (int extra in target.ExtraIndices) target.Action.ApplyBindingOverride(extra, string.Empty);
            }
            return result;
        }

        public string SaveBindingOverrides() => Inputs.asset.SaveBindingOverridesAsJson();

        public string SaveSystemBindingOverrides() => _systemMap.SaveBindingOverridesAsJson();

        /// <summary>Restores saved overrides; overrides for actions that no longer exist are dropped (Decision 16).</summary>
        public void LoadBindingOverrides(string assetJson, string systemJson)
        {
            Inputs.asset.RemoveAllBindingOverrides();
            _systemMap.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(assetJson)) Inputs.asset.LoadBindingOverridesFromJson(assetJson);
            if (!string.IsNullOrEmpty(systemJson)) _systemMap.LoadBindingOverridesFromJson(systemJson);
        }

        public void ResetBindingOverrides() => LoadBindingOverrides(null, null);

        /// <summary>
        /// Listens for the next keyboard key or mouse button 3–5 with every keybind masked (Escape cancels).
        /// Dispose to stop listening. Uses <see cref="InputSystem.onAnyButtonPress"/> rather than
        /// PerformInteractiveRebinding, which would fight <see cref="InputArbiter"/> over action enabled state.
        /// </summary>
        public IDisposable CaptureNextButton(Action<string> captured, Action cancelled)
        {
            KeyCapture capture = new(PushContext(InputContext.TextEntry));
            capture.Listener = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (control.synthetic) return;
                string path = null;
                bool cancel = control is KeyControl { keyCode: UnityEngine.InputSystem.Key.Escape };
                if (!cancel && control is KeyControl) path = "<Keyboard>/" + control.name;
                else if (control.device is Mouse && control.name is "middleButton" or "forwardButton" or "backButton")
                    path = "<Mouse>/" + control.name;
                if (!cancel && path == null) return;

                capture.StopListening();
                // This callback runs inside the press's own event: dropping the mask now would let that same press
                // reach the action it is bound to (Escape would close the menu, B open the build menu).
                StartCoroutine(ReleaseWhenUp(capture, control));
                if (cancel) cancelled?.Invoke();
                else captured?.Invoke(path);
            });
            return capture;
        }

        private static IEnumerator ReleaseWhenUp(KeyCapture capture, InputControl control)
        {
            while (control.IsPressed()) yield return null;
            yield return null;
            capture.Dispose();
        }

        private sealed class KeyCapture : IDisposable
        {
            private IInputHandle _mask;

            public KeyCapture(IInputHandle mask) => _mask = mask;

            public IDisposable Listener;

            public void StopListening()
            {
                Listener?.Dispose();
                Listener = null;
            }

            public void Dispose()
            {
                StopListening();
                _mask?.Dispose();
                _mask = null;
            }
        }

        private void AddRow(List<KeybindRow> rows, string group, string label, InputAction action, int keyIndex, string modifier, bool readOnly)
        {
            string id = $"{action.actionMap?.name}/{action.name}/{keyIndex}";
            int[] extras = action.bindings.Select((b, i) => (b, i))
                .Where(x => x.i != keyIndex && !x.b.isComposite && !x.b.isPartOfComposite).Select(x => x.i).ToArray();
            _rowTargets[id] = new RowTarget(action, keyIndex, extras);
            rows.Add(new KeybindRow(id, label, group, action.bindings[keyIndex].effectivePath, modifier, MaskOf(action), readOnly));
        }

        private static void AddFixedRow(List<KeybindRow> rows, string group, string label, string path, int mask) =>
            rows.Add(new KeybindRow("fixed/" + label, label, group, path, null, mask, true));

        /// <summary>Bit per <see cref="InputContext"/> that enables the action: the arbiter's own notion of "active together".</summary>
        private int MaskOf(InputAction action)
        {
            int mask = 0;
            int bit = 0;
            foreach (KeyValuePair<InputContext, InputContextDefinition> context in _contexts.OrderBy(c => c.Key))
            {
                if (context.Value.Includes(action)) mask |= 1 << bit;
                bit++;
            }
            return mask;
        }
    }
}
