using System;
using System.Collections.Generic;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inputs;
using SS3D.Systems.Roles;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window (lobby.md §6, lobby effort Phase 5), built from <c>CharacterWindow.uxml</c>: a top bar with
    /// the switcher, Saved / Discard / Save and Return to lobby; the preview column with rotation and a show-clothing
    /// toggle; and the Appearance, Jobs and Antags tabs. One Save covers all three (Decision 5). Esc returns to the
    /// lobby and, with unsaved changes, asks Save / Discard / Keep editing; so does any switcher action that would
    /// leave the edited character. <see cref="LobbyUiSubSystem"/> owns the roster, the booth and the network.
    /// </summary>
    public sealed class CharacterWindowView : IUiSurface
    {
        public const string TabAppearance = "appearance";
        public const string TabJobs = "jobs";
        public const string TabAntags = "antags";

        private static readonly string[] TabOrder = { TabAppearance, TabJobs, TabAntags };
        private static readonly string[] AngleLabels = { "Front", "Side", "Back" };

        private readonly LobbyAssetCatalog _catalog;
        private readonly CharacterRoster _roster;
        private readonly Func<string, Texture> _portraitOf;
        private readonly Func<string, Department> _departmentOf;
        private readonly InputTextEntryScope _textEntry = new();
        private readonly CharacterWindowModel _model = new();
        private readonly Dictionary<string, Button> _tabs = new();

        private VisualElement _root;
        private VisualElement _tabContent;
        private CharacterAppearanceTab _appearance;
        private CharacterJobsTab _jobs;
        private CharacterAntagsTab _antags;
        private CharacterSwitcher _switcher;
        private Button _switchButton;
        private VisualElement _switchPortrait;
        private Label _switchName;
        private Label _switchHighPrefix;
        private Label _switchHighJob;
        private Label _switchCount;
        private VisualElement _status;
        private Label _statusLabel;
        private Button _discard;
        private Button _save;
        private VisualElement _previewImage;
        private VisualElement _preview;
        private List<VisualElement> _angles;
        private Button _showClothing;
        private Label _caption;
        private VisualElement _prompt;
        private Label _promptError;
        private Button _promptSave;
        private Action _pending;
        private Texture _previewTexture;
        private string _activeTab = TabAppearance;
        private int _angleIndex;
        private bool _clothingShown = true;
        private string _fallbackJob = "Assistant";

        /// <summary>Return to lobby, after any unsaved changes were saved or discarded.</summary>
        public event Action ReturnRequested;
        /// <summary>Save: the stored character, name normalised. The host puts it in the roster, writes and sends it.</summary>
        public event Action<CharacterCreatorDraft> Saved;
        /// <summary>A switcher choice, once unsaved changes are resolved.</summary>
        public event Action<string> SelectRequested;
        public event Action<string> DuplicateRequested;
        public event Action<string> DeleteRequested;
        public event Action NewRequested;
        /// <summary>0 = Front, 1 = Side, 2 = Back.</summary>
        public event Action<int> AngleChanged;
        /// <summary>The working copy's body sliders changed.</summary>
        public event Action<CharacterCreatorDraft> BodyChanged;
        /// <summary>The working copy's hair, facial hair, eyebrows, skin or eyes changed.</summary>
        public event Action<CharacterCreatorDraft> LookChanged;
        /// <summary>The preview's outfit: the job whose clothes it wears, and whether clothing shows (Decision 14).</summary>
        public event Action<string, bool> OutfitChanged;

        public CharacterWindowView(LobbyAssetCatalog catalog, CharacterRoster roster, Func<string, Texture> portraitOf,
            Func<string, Department> departmentOf)
        {
            _catalog = catalog;
            _roster = roster;
            _portraitOf = portraitOf ?? (_ => null);
            _departmentOf = departmentOf ?? (_ => Department.None);
        }

        public VisualElement Root => _root;

        public bool IsVisible => _root != null && _root.style.display != DisplayStyle.None;

        public string ActiveTab => _activeTab;

        public CharacterWindowModel Model => _model;

        public CharacterSwitcher Switcher => _switcher;

        public bool PromptOpen => _prompt != null && _prompt.ClassListContains("charwin__prompt--open");

        /// <summary>The character the window edits (the roster's selected one).</summary>
        public string CharacterId => _model.Saved?.Id;

        public bool ClothingShown => _clothingShown;

        public void Attach(VisualElement layerRoot)
        {
            VisualTreeAsset template = _catalog != null ? _catalog.CharacterWindowTemplate : null;
            if (template == null)
            {
                Debug.LogError("[Lobby] LobbyAssetCatalog has no character window template. Run SS3D → Data → Rebuild All UI Catalogs.");
                return;
            }

            _root = template.CloneTree().Q("character-window-surface");
            _root.RemoveFromHierarchy();
            _root.pickingMode = PickingMode.Position;
            BindTopBar();
            BindPreview();
            BindTabs();
            BindPrompt();

            _switcher = new CharacterSwitcher(_roster, _portraitOf, _departmentOf);
            _switcher.SelectRequested += id => Guard(id != _roster.SelectedId, () => SelectRequested?.Invoke(id));
            _switcher.NewRequested += () => Guard(true, () => NewRequested?.Invoke());
            _switcher.DuplicateRequested += id => Guard(true, () => DuplicateRequested?.Invoke(id));
            _switcher.DeleteRequested += id => Guard(id == _roster.SelectedId, () => DeleteRequested?.Invoke(id));
            _switcher.Closed += () => _switchButton.RemoveFromClassList("charwin__switch--open");
            _switcher.Mount(_root.Q("charwin-window"), _switchButton);

            _root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                bool compact = e.newRect.width > 0f && e.newRect.width < LobbyView.CompactWidth;
                _root.EnableInClassList("lobby--compact", compact);
                _jobs.SetCompact(compact);
            });
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            layerRoot.Add(_root);
            SetVisible(false);
        }

        public void Detach()
        {
            ReturnRequested = null;
            Saved = null;
            SelectRequested = null;
            DuplicateRequested = null;
            DeleteRequested = null;
            NewRequested = null;
            AngleChanged = null;
            BodyChanged = null;
            LookChanged = null;
            OutfitChanged = null;
            _textEntry.Exit();
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null) return;
            if (!visible)
            {
                _textEntry.Exit();
                _switcher?.Close();
                ClosePrompt();
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        /// <summary>Opens on <paramref name="tab"/> with the selected character; focus starts in the Name field.</summary>
        public void Open(string tab = TabAppearance)
        {
            if (_root == null) return;
            Load(_roster.Selected);
            _angleIndex = 0;
            _clothingShown = true;
            RefreshPreviewControls();
            SelectTab(tab);
            SetVisible(true);
            AngleChanged?.Invoke(_angleIndex);
            RaisePreview();
            _root.schedule.Execute(() =>
            {
                if (_activeTab == TabAppearance) _appearance.NameField?.Focus();
                else _tabs[_activeTab].Focus();
            }).ExecuteLater(1);
        }

        /// <summary>Edits <paramref name="saved"/> from scratch (open, or the roster's selection changed).</summary>
        public void Load(CharacterCreatorDraft saved)
        {
            if (_root == null || saved == null) return;
            _model.Load(saved);
            _appearance.Rebuild();
            _jobs.Rebuild();
            _antags.Rebuild();
            RefreshTopBar();
            if (IsVisible) RaisePreview();
        }

        /// <summary>Repaints the top bar, the outfit caption and the tabs' state from the working copy.</summary>
        public void Refresh()
        {
            if (_root == null) return;
            RefreshTopBar();
            RefreshPreviewControls();
            _appearance.Refresh();
            _jobs.Refresh();
        }

        /// <summary>The roster changed (count, names, portraits): repaint the top-bar button and an open switcher.</summary>
        public void RefreshRoster()
        {
            if (_root == null) return;
            RefreshTopBar();
            if (_switcher.IsOpen) _switcher.Refresh();
        }

        /// <summary>The server's jobs, their descriptions and the fallback job (Assistant).</summary>
        public void SetJobs(IReadOnlyList<JobSlotInfo> jobs, Func<string, string> describe, string fallbackJob)
        {
            if (_root == null) return;
            _fallbackJob = string.IsNullOrEmpty(fallbackJob) ? "Assistant" : fallbackJob;
            // No job has a playtime requirement yet (effort deviation): every row is open.
            _jobs.SetJobs(jobs, describe, _ => null, _fallbackJob);
            _jobs.Rebuild();
            RefreshPreviewControls();
        }

        public void SetAntagonistDefault(bool optedIn) => _antags?.SetServerDefault(optedIn);

        public void SetPreviewTexture(Texture texture)
        {
            _previewTexture = texture;
            if (_previewImage == null) return;
            _previewImage.style.backgroundImage = texture switch
            {
                RenderTexture rt => Background.FromRenderTexture(rt),
                Texture2D tex => new StyleBackground(tex),
                _ => StyleKeyword.None,
            };
        }

        // ---- Binding ----

        private void BindTopBar()
        {
            _switchButton = _root.Q<Button>("charwin-switch");
            _switchButton.clicked += () =>
            {
                _switcher.Toggle(_switchButton, true);
                _switchButton.EnableInClassList("charwin__switch--open", _switcher.IsOpen);
            };
            _switchPortrait = _root.Q("charwin-switch-portrait");
            _switchName = _root.Q<Label>("charwin-switch-name");
            _switchHighPrefix = _root.Q<Label>("charwin-switch-high-prefix");
            _switchHighJob = _root.Q<Label>("charwin-switch-high-job");
            _switchCount = _root.Q<Label>("charwin-switch-count");
            _status = _root.Q("charwin-status");
            _statusLabel = _root.Q<Label>("charwin-status-label");
            _discard = _root.Q<Button>("charwin-discard");
            _discard.clicked += Discard;
            _save = _root.Q<Button>("charwin-save");
            _save.clicked += () => Save();
            _root.Q<Button>("charwin-return").clicked += RequestReturn;
        }

        private void BindPreview()
        {
            _preview = _root.Q("charwin-preview");
            _previewImage = _root.Q("charwin-preview-image");
            _angles = _root.Q("charwin-angles").Query(className: "charwin__angle").ToList();
            _root.Q<Button>("charwin-rotate-left").clicked += () => Rotate(-1);
            _root.Q<Button>("charwin-rotate-right").clicked += () => Rotate(1);
            _preview.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.LeftArrow or KeyCode.RightArrow)) return;
                Rotate(e.keyCode == KeyCode.LeftArrow ? -1 : 1);
                e.StopPropagation();
            });
            _showClothing = _root.Q<Button>("charwin-show-clothing");
            _showClothing.clicked += () =>
            {
                _clothingShown = !_clothingShown;
                RefreshPreviewControls();
                RaiseOutfit();
            };
            _caption = _root.Q<Label>("charwin-outfit-caption");
            SetPreviewTexture(_previewTexture);
        }

        private void BindTabs()
        {
            _tabContent = _root.Q("charwin-tab-content");
            foreach (string key in TabOrder)
            {
                Button tab = _root.Q<Button>("charwin-tab-" + key);
                string captured = key;
                tab.clicked += () => SelectTab(captured);
                _tabs[key] = tab;
            }

            _appearance = new CharacterAppearanceTab(_model, _catalog, _textEntry,
                () => RandomCharacter.RandomName(_catalog != null ? _catalog.CharacterNames : null, Random),
                draft => RandomCharacter.RandomizeLook(draft, RandomCharacter.LookOptions.From(_catalog), Random));
            _appearance.Changed += change =>
            {
                RefreshTopBar();
                if (change == CharacterLookChange.Body) BodyChanged?.Invoke(_model.Working);
                else if (change == CharacterLookChange.Look) LookChanged?.Invoke(_model.Working);
            };
            _jobs = new CharacterJobsTab(_model, _catalog);
            _jobs.Changed += () =>
            {
                RefreshTopBar();
                RefreshPreviewControls();
                RaiseOutfit();
            };
            _antags = new CharacterAntagsTab(_model);
            _antags.Changed += RefreshTopBar;
        }

        private static readonly System.Random Random = new();

        private void BindPrompt()
        {
            _prompt = _root.Q("charwin-prompt");
            _promptError = _root.Q<Label>("charwin-prompt-error");
            _promptSave = _root.Q<Button>("charwin-prompt-save");
            _promptSave.clicked += () =>
            {
                Action next = _pending;
                if (!Save()) return;
                ClosePrompt();
                next?.Invoke();
            };
            _root.Q<Button>("charwin-prompt-discard").clicked += () =>
            {
                Action next = _pending;
                ClosePrompt();
                Discard();
                next?.Invoke();
            };
            _root.Q<Button>("charwin-prompt-keep").clicked += ClosePrompt;
        }

        // ---- Tabs ----

        public void SelectTab(string key)
        {
            if (!_tabs.ContainsKey(key)) key = TabAppearance;
            _activeTab = key;
            foreach ((string each, Button tab) in _tabs) tab.EnableInClassList("lobby__tab--active", each == key);
            // A focused field removed from the panel gets no FocusOut.
            _textEntry.Exit();
            _tabContent.Clear();
            _tabContent.Add(key switch
            {
                TabJobs => _jobs.Root,
                TabAntags => _antags.Root,
                _ => _appearance.Root,
            });
        }

        // ---- Save, discard, return ----

        /// <summary>Saves the working copy; false when there is nothing to save or the name is invalid.</summary>
        public bool Save()
        {
            CharacterCreatorDraft saved = _model.Commit();
            if (saved == null)
            {
                if (PromptOpen) ShowPromptError();
                return false;
            }

            // The normalised name goes back into the field.
            _appearance.NameField?.SetValueWithoutNotify(saved.Name);
            RefreshTopBar();
            Saved?.Invoke(saved);
            return true;
        }

        public void Discard()
        {
            if (!_model.IsDirty) return;
            _model.Discard();
            Load(_model.Saved);
        }

        public void RequestReturn() => Guard(true, () => ReturnRequested?.Invoke());

        /// <summary>
        /// Runs <paramref name="action"/> now, or asks Save / Discard / Keep editing first when it would leave unsaved
        /// changes (<paramref name="leavesCharacter"/>).
        /// </summary>
        private void Guard(bool leavesCharacter, Action action)
        {
            if (!leavesCharacter || !_model.IsDirty)
            {
                action();
                return;
            }

            _pending = action;
            _switcher.Close();
            _prompt.AddToClassList("charwin__prompt--open");
            ShowPromptError();
            _prompt.schedule.Execute(() => (_model.NameValid ? _promptSave : _root.Q<Button>("charwin-prompt-keep")).Focus()).ExecuteLater(1);
        }

        private void ShowPromptError()
        {
            bool valid = _model.NameValid;
            _promptSave.SetEnabled(valid);
            _promptError.text = valid ? "" : "Can't save: " + SS3D.Systems.Entities.Character.CharacterNameRules.Describe(_model.NameError);
            _promptError.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ClosePrompt()
        {
            _pending = null;
            _prompt?.RemoveFromClassList("charwin__prompt--open");
        }

        // ---- Keys ----

        private void OnKeyDown(KeyDownEvent e)
        {
            VisualElement focused = _root.panel?.focusController?.focusedElement as VisualElement;
            bool typing = focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null;
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                {
                    if (PromptOpen) ClosePrompt();
                    // Inside the popover, Esc is the switcher's (it backs out of a delete confirm first).
                    else if (_switcher.IsOpen && focused != null && _switcher.Root.Contains(focused)) return;
                    else if (_switcher.IsOpen)
                    {
                        _switcher.Close();
                        _switchButton.Focus();
                    }
                    else RequestReturn();
                    break;
                }
                case KeyCode.S when e.ctrlKey || e.commandKey:
                {
                    if (!PromptOpen) Save();
                    break;
                }
                case KeyCode.Tab when e.ctrlKey:
                {
                    if (PromptOpen) return;
                    int index = Array.IndexOf(TabOrder, _activeTab);
                    string next = TabOrder[(index + (e.shiftKey ? TabOrder.Length - 1 : 1)) % TabOrder.Length];
                    SelectTab(next);
                    _tabs[next].Focus();
                    break;
                }
                case KeyCode.Q when !typing && !e.ctrlKey && !PromptOpen:
                {
                    Rotate(-1);
                    break;
                }
                case KeyCode.E when !typing && !e.ctrlKey && !PromptOpen:
                {
                    Rotate(1);
                    break;
                }
                default:
                {
                    return;
                }
            }

            e.StopPropagation();
        }

        // ---- Preview ----

        private void Rotate(int step)
        {
            _angleIndex = (_angleIndex + step + AngleLabels.Length) % AngleLabels.Length;
            RefreshPreviewControls();
            AngleChanged?.Invoke(_angleIndex);
        }

        private void RefreshPreviewControls()
        {
            if (_angles == null) return;
            for (int i = 0; i < _angles.Count; i++) _angles[i].EnableInClassList("charwin__angle--active", i == _angleIndex);
            _preview.tooltip = $"Character preview, {AngleLabels[_angleIndex]} view. Left and right arrows rotate it.";
            _showClothing.EnableInClassList("lobby__switch-toggle--on", _clothingShown);
            _showClothing.tooltip = _clothingShown ? "Hide clothing" : "Show clothing";
            if (_model.Working != null) _caption.text = _model.OutfitCaption(_fallbackJob, _clothingShown);
        }

        private void RaisePreview()
        {
            BodyChanged?.Invoke(_model.Working);
            LookChanged?.Invoke(_model.Working);
            RaiseOutfit();
        }

        private void RaiseOutfit() => OutfitChanged?.Invoke(_model.OutfitJob(_fallbackJob), _clothingShown);

        // ---- Top bar ----

        private void RefreshTopBar()
        {
            CharacterCreatorDraft working = _model.Working;
            if (working == null) return;
            CharacterCreatorDraft saved = _model.Saved;
            _switchName.text = string.IsNullOrWhiteSpace(saved.Name) ? "Unnamed" : saved.Name;
            string high = saved.Preferences?.HighJob;
            _switchHighPrefix.text = high != null ? "High:" : "No High job";
            _switchHighJob.text = high ?? "";
            foreach (Department each in Enum.GetValues(typeof(Department)))
            {
                string cls = LobbyView.DepartmentClass(each);
                if (cls != null) _switchHighJob.EnableInClassList(cls, high != null && _departmentOf(high) == each);
            }

            _switchCount.text = $"{_roster.Count}/{CharacterRoster.MaxCharacters}";
            _switchButton.tooltip = "Switch character, current " + _switchName.text;
            Texture portrait = _portraitOf(saved.Id);
            _switchPortrait.style.backgroundImage = portrait switch
            {
                RenderTexture rt => Background.FromRenderTexture(rt),
                Texture2D tex => new StyleBackground(tex),
                _ => StyleKeyword.None,
            };

            bool dirty = _model.IsDirty;
            _status.EnableInClassList("charwin__status--dirty", dirty);
            _statusLabel.text = dirty ? "Unsaved changes" : "Saved";
            _discard.SetEnabled(dirty);
            _save.SetEnabled(_model.CanSave);
            _save.tooltip = _model.SaveTooltip;
        }
    }
}
