using System;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The admin's Stop confirmation while the map editor has unsaved edits (A4): Save and stop, Discard and stop or
    /// Cancel. It covers the whole lobby; Escape cancels.
    /// </summary>
    public sealed class LobbyMapStopPrompt
    {
        private readonly VisualElement _card;
        private readonly Label _text;
        private readonly Label _error;
        private readonly TextField _name;
        private int _token;

        /// <summary>The prompt's token, the chosen action and the map name typed for an unnamed map.</summary>
        public event Action<int, MapStopChoice, string> Resolved;

        public VisualElement Root { get; }

        public LobbyMapStopPrompt(InputTextEntryScope textEntry)
        {
            Root = LobbyUi.Box("lobby-prompt");
            Root.name = "map-stop-prompt";
            Root.style.display = DisplayStyle.None;
            _card = LobbyUi.Box("lobby-prompt__card");
            _card.Add(LobbyUi.Titling("Unsaved map changes", "lobby-prompt__title"));
            _text = LobbyUi.Text("", "lobby-prompt__text");
            _card.Add(_text);
            _name = new TextField("New map name") { name = "map-stop-name", maxLength = 80 };
            _name.AddToClassList("lobby-admin__text");
            LobbyUi.DarkInput(_name);
            LobbyUi.HoldTextEntry(_name, textEntry);
            _card.Add(_name);
            _error = LobbyUi.Text("", "lobby-prompt__error");
            _card.Add(_error);
            VisualElement buttons = LobbyUi.Box("lobby-prompt__buttons");
            foreach ((string name, string label, MapStopChoice choice) in new (string, string, MapStopChoice)[]
            {
                ("map-stop-save", "Save and stop", MapStopChoice.SaveAndStop),
                ("map-stop-discard", "Discard and stop", MapStopChoice.DiscardAndStop),
                ("map-stop-cancel", "Cancel", MapStopChoice.Cancel),
            })
            {
                Button button = new(() => Resolve(choice)) { name = name, text = label.ToUpperInvariant() };
                button.AddToClassList("lobby-admin__button");
                button.AddToClassList("lb-titling");
                if (choice == MapStopChoice.SaveAndStop) button.AddToClassList("lobby-admin__button--start");
                buttons.Add(button);
            }

            _card.Add(buttons);
            Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape) return;
                Resolve(MapStopChoice.Cancel);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            Root.Add(_card);
        }

        public bool IsOpen => _token != 0;

        /// <summary>Shows the prompt for <paramref name="token"/>; 0 hides it.</summary>
        public void Set(int token, string editingMap, string error)
        {
            bool changed = token != _token;
            _token = token;
            Root.style.display = token == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (token == 0) return;
            _card.SetEnabled(true);
            bool unnamed = string.IsNullOrEmpty(editingMap);
            _text.text = unnamed
                ? "This new map has unsaved changes. Stopping the round unloads the station. Save and stop saves it under a new name first; Discard loses these edits."
                : $"{editingMap} has unsaved changes. Stopping the round unloads the station. Save and stop overwrites {editingMap} first; Discard loses these edits.";
            _name.style.display = unnamed ? DisplayStyle.Flex : DisplayStyle.None;
            if (changed) _name.SetValueWithoutNotify(unnamed ? "" : editingMap);
            _error.text = error ?? "";
            if (changed) Root.Q<Button>("map-stop-cancel").Focus();
        }

        /// <summary>Forgets the open prompt (the shell detached).</summary>
        public void Reset()
        {
            _token = 0;
            Resolved = null;
        }

        private void Resolve(MapStopChoice choice)
        {
            if (_token == 0 || !_card.enabledSelf) return;
            _name.Blur();
            _card.SetEnabled(false);
            Resolved?.Invoke(_token, choice, _name.value);
        }
    }
}
