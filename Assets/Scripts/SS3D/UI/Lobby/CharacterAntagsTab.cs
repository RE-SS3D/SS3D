using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Roles;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window's Antags tab (lobby.md §4, lobby effort Phase 5), wide over the viewport while open: one row
    /// per antagonist role the server can run, with its description and Off / On, and the note that the lobby never shows
    /// which mode will run. A role the character never set shows the server's default (off, except on the
    /// <c>-nukeops</c> profile). Off / On repaints its row in place, so the opted-in look eases in.
    /// </summary>
    public sealed class CharacterAntagsTab
    {
        private readonly CharacterWindowModel _model;
        private readonly ScrollView _scroll;
        private readonly Dictionary<string, VisualElement> _rows = new();
        private bool _serverDefault;

        public event Action Changed;

        public CharacterAntagsTab(CharacterWindowModel model)
        {
            _model = model;
            _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "charwin-antags" };
            _scroll.AddToClassList("charwin-antags");
            _scroll.AddToClassList("charwin__pane");
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        public VisualElement Root => _scroll;

        /// <summary>True while the working copy is switched on for any role (the window marks the Antags tab).</summary>
        public bool AnyOn => _model.Working != null
            && AntagonistCategories.All.Any(role => _model.Working.Preferences.IsAntagonistOn(role.Key, _serverDefault));

        /// <summary>What an unset role does on this server (<c>ServerSettingsService.AntagonistOptInDefault</c>).</summary>
        public void SetServerDefault(bool optedIn)
        {
            if (_serverDefault == optedIn) return;
            _serverDefault = optedIn;
            Rebuild();
        }

        public void Rebuild()
        {
            _scroll.Clear();
            _rows.Clear();
            if (_model.Working == null) return;

            _scroll.Add(LobbyUi.Text("antagonist eligibility", "charwin-antags__title"));
            Label intro = LobbyUi.Text("Switching a role on lets the round pick you for it. It is not a job: you still spawn in your "
                + "job, and most rounds you will never be drawn. "
                + (_serverDefault ? "On this server a role you haven't set starts on." : "Every role starts off."), "charwin__note");
            intro.AddToClassList("charwin-antags__intro");
            _scroll.Add(intro);

            VisualElement note = LobbyUi.Box("charwin__notice");
            note.name = "charwin-antags-note";
            note.Add(LobbyUi.Icon("info"));
            note.Add(LobbyUi.Text("These roles are only drawn if their gamemode is picked for the round. The lobby never shows "
                + "which mode will run, so a \"Secret\" round stays secret.", "charwin__notice-text"));
            _scroll.Add(note);

            foreach ((string key, string name, string description) in AntagonistCategories.All)
            {
                VisualElement row = BuildRow(key, name, description);
                _rows[key] = row;
                _scroll.Add(row);
            }

            Refresh();
        }

        /// <summary>Repaints each row's Off / On from the working copy, without rebuilding.</summary>
        private void Refresh()
        {
            foreach ((string key, VisualElement row) in _rows)
            {
                bool on = _model.Working.Preferences.IsAntagonistOn(key, _serverDefault);
                row.EnableInClassList("charwin-antags__row--on", on);
                row.Q<Button>("charwin-antag-off").EnableInClassList("charwin__seg-option--on", !on);
                row.Q<Button>("charwin-antag-on").EnableInClassList("charwin__seg-option--on", on);
            }
        }

        private VisualElement BuildRow(string key, string name, string description)
        {
            VisualElement row = LobbyUi.Box("charwin-antags__row");
            row.name = "charwin-antag-" + key;
            VisualElement frame = LobbyUi.Box("charwin-antags__icon-frame");
            frame.Add(LobbyUi.Box("lobby__icon", "charwin-antags__icon"));
            row.Add(frame);
            VisualElement text = LobbyUi.Box("charwin-antags__text");
            text.Add(LobbyUi.Text(name.ToLowerInvariant(), "charwin-antags__name"));
            text.Add(LobbyUi.Text(description, "charwin-antags__description"));
            row.Add(text);

            VisualElement choice = LobbyUi.Box("charwin__seg");
            choice.Add(Option("charwin-antag-off", "off", "Never be drawn as " + name, key, false));
            choice.Add(Option("charwin-antag-on", "on", "Be considered for " + name, key, true));
            row.Add(choice);
            return row;
        }

        private Button Option(string name, string label, string tooltip, string key, bool value)
        {
            Button option = new(() =>
            {
                if (_model.Working.Preferences.IsAntagonistOn(key, _serverDefault) == value) return;
                _model.Working.Preferences.SetAntagonist(key, value);
                Refresh();
                Changed?.Invoke();
            }) { name = name, text = label, tooltip = tooltip };
            option.AddToClassList("charwin__seg-option");
            // On is a red plate when chosen, a chosen Off a muted one (CharacterWindow.uss, Antags).
            option.AddToClassList(value ? "charwin-antags__option--on" : "charwin-antags__option--off");
            return option;
        }
    }
}
