using System;
using SS3D.Systems.Roles;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window's Antags tab (lobby.md §4, lobby effort Phase 5): one row per antagonist role the server can
    /// run, with its description and a switch, and the note that the lobby never shows which mode will run. A role the
    /// character never set shows the server's default (off, except on the <c>-nukeops</c> profile).
    /// </summary>
    public sealed class CharacterAntagsTab
    {
        private readonly CharacterWindowModel _model;
        private readonly ScrollView _scroll;
        private bool _serverDefault;

        public event Action Changed;

        public CharacterAntagsTab(CharacterWindowModel model)
        {
            _model = model;
            Root = LobbyUi.Box("charwin-tab");
            Root.name = "charwin-antags";
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("charwin-tab__scroll");
            Root.Add(_scroll);
        }

        public VisualElement Root { get; }

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
            if (_model.Working == null) return;

            VisualElement box = LobbyUi.Box("charwin-antags");
            box.Add(LobbyUi.Titling("Antagonist eligibility", "charwin-tab__heading"));
            box.Add(LobbyUi.Text("Switching a role on lets the round pick you for it. It is not a job: you still spawn in your "
                + "job, and most rounds you will never be drawn. "
                + (_serverDefault ? "On this server a role you haven't set starts on." : "Every role starts off."), "charwin-tab__intro"));

            VisualElement note = LobbyUi.Box("charwin__notice");
            note.name = "charwin-antags-note";
            note.Add(LobbyUi.Icon("info"));
            note.Add(LobbyUi.Text("These roles are only drawn if their gamemode is picked for the round. The lobby never shows "
                + "which mode will run, so a \"Secret\" round stays secret.", "charwin__notice-text"));
            box.Add(note);

            foreach ((string key, string name, string description) in AntagonistCategories.All)
            {
                box.Add(BuildRow(key, name, description));
            }

            _scroll.Add(box);
        }

        private VisualElement BuildRow(string key, string name, string description)
        {
            bool on = _model.Working.Preferences.IsAntagonistOn(key, _serverDefault);
            VisualElement row = LobbyUi.Box("charwin-antags__row");
            row.name = "charwin-antag-" + key;
            row.EnableInClassList("charwin-antags__row--on", on);
            VisualElement frame = LobbyUi.Box("charwin-antags__icon-frame");
            frame.Add(LobbyUi.Box("lobby__icon", "charwin-antags__icon"));
            row.Add(frame);
            VisualElement text = LobbyUi.Box("charwin-antags__text");
            text.Add(LobbyUi.Titling(name, "charwin-antags__name"));
            text.Add(LobbyUi.Text(description, "charwin-antags__description"));
            row.Add(text);
            row.Add(LobbyUi.Text(on ? "Opted in" : "Off", "charwin-antags__state"));
            Button toggle = new(() =>
            {
                _model.Working.Preferences.SetAntagonist(key, !_model.Working.Preferences.IsAntagonistOn(key, _serverDefault));
                Rebuild();
                Changed?.Invoke();
            }) { name = "charwin-antag-switch", tooltip = "Be considered for " + name };
            toggle.AddToClassList("lobby__switch-toggle");
            toggle.EnableInClassList("lobby__switch-toggle--on", on);
            toggle.Add(LobbyUi.Box("lobby__switch-knob"));
            row.Add(toggle);
            return row;
        }
    }
}
