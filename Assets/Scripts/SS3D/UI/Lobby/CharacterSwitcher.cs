using System;
using System.Collections.Generic;
using SS3D.Systems.IdAccess;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character switcher popover (lobby.md §6, lobby effort Phase 5). It opens beside "Switch character" in the
    /// lobby sidebar and below the character window's top-bar button. Select, New, Duplicate and Delete (with a confirm
    /// step that names the character) are raised as events; the host changes the roster and calls
    /// <see cref="Refresh"/>. Keys: ↑/↓ move, Enter selects and closes, Delete starts the confirm, Esc closes.
    /// </summary>
    public sealed class CharacterSwitcher
    {
        private readonly CharacterRoster _roster;
        private readonly Func<string, Texture> _portraitOf;
        private readonly Func<string, Department> _departmentOf;
        private readonly ScrollView _list;
        private readonly Label _count;
        private readonly Button _new;
        private readonly Label _full;
        private readonly List<VisualElement> _rows = new();
        private VisualElement _anchor;
        private bool _below;
        private string _confirmId;

        public event Action<string> SelectRequested;
        public event Action<string> DuplicateRequested;
        public event Action<string> DeleteRequested;
        public event Action NewRequested;
        /// <summary>The popover closed (Esc, a click outside, or a choice).</summary>
        public event Action Closed;

        /// <param name="roster">The characters shown; the host changes it and calls <see cref="Refresh"/>.</param>
        /// <param name="portraitOf">A rendered head crop for a character id, or null for the silhouette.</param>
        /// <param name="departmentOf">The department of a job, for the High job's colour.</param>
        public CharacterSwitcher(CharacterRoster roster, Func<string, Texture> portraitOf, Func<string, Department> departmentOf)
        {
            _roster = roster;
            _portraitOf = portraitOf ?? (_ => null);
            _departmentOf = departmentOf ?? (_ => Department.None);

            Root = LobbyUi.Box("switcher");
            Root.name = "character-switcher";
            Root.pickingMode = PickingMode.Position;

            VisualElement head = LobbyUi.Box("switcher__head");
            head.Add(LobbyUi.Titling("Characters", "switcher__title"));
            _count = LobbyUi.Mono("", "switcher__count");
            head.Add(_count);
            head.Add(LobbyUi.Box("lobby__grow"));
            head.Add(LobbyUi.Text("Click to select", "switcher__hint"));
            Root.Add(head);

            _list = new ScrollView(ScrollViewMode.Vertical) { name = "character-switcher-list" };
            _list.AddToClassList("switcher__list");
            Root.Add(_list);

            VisualElement foot = LobbyUi.Box("switcher__foot");
            _new = new Button(() => NewRequested?.Invoke()) { name = "character-switcher-new" };
            _new.AddToClassList("switcher__new");
            _new.Add(LobbyUi.Icon("plus"));
            _new.Add(LobbyUi.Titling("New character", "switcher__new-label"));
            foot.Add(_new);
            _full = LobbyUi.Text("", "switcher__full");
            foot.Add(_full);
            Root.Add(foot);

            Root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            Root.RegisterCallback<GeometryChangedEvent>(_ => Place());
        }

        public VisualElement Root { get; }

        /// <summary>
        /// Puts the popover in <paramref name="host"/> and closes it on a press anywhere else in the host, except on
        /// <paramref name="toggles"/>, which toggle it themselves.
        /// </summary>
        public void Mount(VisualElement host, params VisualElement[] toggles)
        {
            host.Add(Root);
            host.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsOpen || e.target is not VisualElement target) return;
                if (Root.Contains(target)) return;
                foreach (VisualElement toggle in toggles)
                    if (toggle != null && toggle.Contains(target)) return;
                Close();
            }, TrickleDown.TrickleDown);
        }

        public bool IsOpen => Root.ClassListContains("switcher--open");

        /// <summary>The character whose Delete is waiting for confirmation, or null.</summary>
        public string ConfirmingDeleteId => _confirmId;

        /// <summary>
        /// Opens next to <paramref name="anchor"/>: to its right (the lobby sidebar) or, with <paramref name="below"/>,
        /// under it (the window top bar). The popover must already sit in the anchor's panel.
        /// </summary>
        public void Open(VisualElement anchor, bool below)
        {
            _anchor = anchor;
            _below = below;
            _confirmId = null;
            Root.AddToClassList("switcher--open");
            Root.BringToFront();
            Refresh();
            Place();
            Root.schedule.Execute(FocusSelected).ExecuteLater(1);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _confirmId = null;
            Root.RemoveFromClassList("switcher--open");
            Closed?.Invoke();
        }

        public void Toggle(VisualElement anchor, bool below)
        {
            if (IsOpen) Close();
            else Open(anchor, below);
        }

        /// <summary>Rebuilds the rows from the roster (after a change, or a new portrait).</summary>
        public void Refresh()
        {
            _rows.Clear();
            _list.Clear();
            _count.text = $"{_roster.Count}/{CharacterRoster.MaxCharacters}";
            foreach (CharacterCreatorDraft character in _roster.Characters)
            {
                VisualElement row = BuildRow(character);
                _rows.Add(row);
                _list.Add(row);
                if (character.Id == _confirmId) _list.Add(BuildConfirm(character));
            }

            _new.SetEnabled(_roster.CanAdd);
            _new.tooltip = _roster.CanAdd ? "Make a new random character" : "";
            _full.text = $"You have {_roster.Count} of {CharacterRoster.MaxCharacters} characters. Delete one to make a new one.";
            _full.EnableInClassList("switcher__full--hidden", _roster.CanAdd);
        }

        /// <summary>Asks to delete <paramref name="id"/>: shows the confirm step under its row, focus on Cancel.</summary>
        public void BeginDelete(string id)
        {
            if (!_roster.CanDelete || _roster.Find(id) == null) return;
            _confirmId = id;
            Refresh();
            Root.schedule.Execute(() => Root.Q<Button>("character-switcher-cancel")?.Focus()).ExecuteLater(1);
        }

        private VisualElement BuildRow(CharacterCreatorDraft character)
        {
            string id = character.Id;
            bool selected = id == _roster.SelectedId;
            VisualElement row = LobbyUi.Box("switcher__row");
            row.name = "character-switcher-row";
            row.userData = id;
            row.focusable = true;
            row.pickingMode = PickingMode.Position;
            row.EnableInClassList("switcher__row--selected", selected);
            row.tooltip = selected ? "Playing as " + character.Name : "Switch to " + character.Name;
            row.RegisterCallback<ClickEvent>(_ => Choose(id));

            VisualElement portrait = LobbyUi.Box("switcher__portrait");
            portrait.pickingMode = PickingMode.Ignore;
            Texture texture = _portraitOf(id);
            if (texture != null)
            {
                VisualElement image = LobbyUi.Box("switcher__portrait-image");
                image.pickingMode = PickingMode.Ignore;
                image.style.backgroundImage = texture is RenderTexture rt ? Background.FromRenderTexture(rt) : new StyleBackground(texture as Texture2D);
                portrait.Add(image);
            }
            else
            {
                VisualElement silhouette = LobbyUi.Icon("user");
                silhouette.AddToClassList("switcher__silhouette");
                portrait.Add(silhouette);
            }

            row.Add(portrait);

            VisualElement text = LobbyUi.Box("switcher__text");
            text.pickingMode = PickingMode.Ignore;
            text.Add(LobbyUi.Text(character.Name, "switcher__name"));
            VisualElement high = LobbyUi.Box("switcher__high");
            string job = character.Preferences?.HighJob;
            if (job != null)
            {
                high.Add(LobbyUi.Text("High:", "switcher__high-prefix"));
                Label jobLabel = LobbyUi.Text(job, "switcher__high-job");
                string dept = LobbyView.DepartmentClass(_departmentOf(job));
                if (dept != null) jobLabel.AddToClassList(dept);
                high.Add(jobLabel);
            }
            else
            {
                high.Add(LobbyUi.Text("No High job", "switcher__high-prefix"));
            }

            text.Add(high);
            row.Add(text);

            if (selected)
            {
                VisualElement mark = LobbyUi.Box("switcher__selected");
                mark.pickingMode = PickingMode.Ignore;
                mark.Add(LobbyUi.Icon("check"));
                mark.Add(LobbyUi.Text("Selected", "switcher__selected-label"));
                row.Add(mark);
            }

            row.Add(Action("duplicate", "Duplicate " + character.Name, _roster.CanAdd, () => DuplicateRequested?.Invoke(id)));
            row.Add(Action("trash", _roster.CanDelete ? "Delete " + character.Name : "You can't delete your only character",
                _roster.CanDelete, () => BeginDelete(id)));
            return row;
        }

        private static Button Action(string icon, string tooltip, bool enabled, System.Action onClick)
        {
            Button button = new() { tooltip = tooltip, name = "character-switcher-" + icon };
            button.AddToClassList("switcher__action");
            button.Add(LobbyUi.Icon(icon));
            button.SetEnabled(enabled);
            button.clicked += onClick;
            // The row selects on click; an action's click must not reach it.
            button.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            return button;
        }

        private VisualElement BuildConfirm(CharacterCreatorDraft character)
        {
            string id = character.Id;
            VisualElement confirm = LobbyUi.Box("switcher__confirm");
            confirm.name = "character-switcher-confirm";
            Label text = LobbyUi.Text("", "switcher__confirm-text");
            text.enableRichText = true;
            text.text = $"Delete <b>{Escape(character.Name)}</b>? Their look and job preferences are removed. This can't be undone.";
            confirm.Add(text);
            VisualElement buttons = LobbyUi.Box("switcher__confirm-buttons");
            Button cancel = new(() =>
            {
                _confirmId = null;
                Refresh();
                FocusRow(id);
            }) { name = "character-switcher-cancel" };
            cancel.AddToClassList("switcher__confirm-button");
            cancel.Add(LobbyUi.Titling("Cancel", "switcher__confirm-label"));
            buttons.Add(cancel);
            Button delete = new(() =>
            {
                _confirmId = null;
                DeleteRequested?.Invoke(id);
            }) { name = "character-switcher-confirm-delete" };
            delete.AddToClassList("switcher__confirm-button");
            delete.AddToClassList("switcher__confirm-button--danger");
            delete.Add(LobbyUi.Icon("trash"));
            delete.Add(LobbyUi.Titling("Delete " + character.Name, "switcher__confirm-label"));
            buttons.Add(delete);
            confirm.Add(buttons);
            return confirm;
        }

        /// <summary>Rich text is on for the confirm line only; a name can't hold markup.</summary>
        private static string Escape(string text) => (text ?? "").Replace("<", "").Replace(">", "");

        private void Choose(string id)
        {
            Close();
            SelectRequested?.Invoke(id);
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            VisualElement focused = Root.panel?.focusController?.focusedElement as VisualElement;
            int index = _rows.FindIndex(row => row == focused);
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                {
                    if (_confirmId != null)
                    {
                        string id = _confirmId;
                        _confirmId = null;
                        Refresh();
                        FocusRow(id);
                    }
                    else
                    {
                        Close();
                        _anchor?.Focus();
                    }

                    break;
                }
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                {
                    if (_rows.Count == 0) return;
                    int step = e.keyCode == KeyCode.UpArrow ? -1 : 1;
                    int next = index < 0 ? 0 : Mathf.Clamp(index + step, 0, _rows.Count - 1);
                    _rows[next].Focus();
                    _list.ScrollTo(_rows[next]);
                    break;
                }
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                {
                    if (index < 0) return;
                    Choose((string)_rows[index].userData);
                    break;
                }
                case KeyCode.Delete:
                {
                    if (index < 0) return;
                    BeginDelete((string)_rows[index].userData);
                    break;
                }
                default:
                {
                    return;
                }
            }

            e.StopPropagation();
        }

        private void FocusSelected() => FocusRow(_roster.SelectedId);

        private void FocusRow(string id)
        {
            VisualElement row = _rows.Find(r => (string)r.userData == id);
            row?.Focus();
        }

        /// <summary>Sits beside or under the anchor, kept inside the host.</summary>
        private void Place()
        {
            if (!IsOpen || _anchor == null || Root.parent == null || _anchor.panel == null) return;
            Rect anchor = Root.parent.WorldToLocal(_anchor.worldBound);
            Rect host = Root.parent.contentRect;
            float height = Root.resolvedStyle.height;
            float left = _below ? anchor.xMin - 48f : anchor.xMax + 8f;
            float top = _below ? anchor.yMax + 4f : anchor.yMax + 85f - (float.IsNaN(height) ? 0f : height);
            if (!float.IsNaN(height) && top + height > host.height - 8f) top = host.height - 8f - height;
            Root.style.left = Mathf.Max(8f, Mathf.Min(left, host.width - 448f));
            Root.style.top = Mathf.Max(8f, top);
        }
    }
}
