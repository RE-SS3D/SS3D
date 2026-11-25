using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inputs;
using SS3D.Systems.Roles;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The character window (lobby.md §6, lobby effort Phase 5), built from <c>CharacterWindow.uxml</c> in the UI bible's
    /// "character" tab look (the old character setup screen): the title with the switcher at its end; a tab column
    /// (Profile, Body, Skin &amp; eyes, Hair, Facial hair, Jobs, Antags); the appearance panes in the centre column beside the
    /// framed preview, which turns to Front / Side / Back or by dragging; Jobs and Antags wide over the preview; save
    /// state with Discard and Save; Return to lobby. One Save covers every tab (Decision 5). Esc returns to the lobby
    /// and, with unsaved changes, asks Save / Discard / Keep editing; so does any switcher action that would leave the
    /// edited character. <see cref="LobbyUiSubSystem"/> owns the roster, the booth and the network.
    /// </summary>
    public sealed class CharacterWindowView : IUiSurface
    {
        public const string TabProfile = "profile";
        public const string TabBody = "body";
        public const string TabSkin = "skin";
        public const string TabHair = "hair";
        public const string TabBeard = "beard";
        public const string TabJobs = "jobs";
        public const string TabAntags = "antags";

        private static readonly string[] TabOrder = { TabProfile, TabBody, TabSkin, TabHair, TabBeard, TabJobs, TabAntags };
        private static readonly string[] WideTabs = { TabJobs, TabAntags };
        private static readonly string[] AngleLabels = { "Front", "Side", "Back" };

        /// <summary>The job outfit preview's first two choices; the server's jobs follow.</summary>
        private const int OutfitHighJob = 0;
        private const int OutfitNaked = 1;

        /// <summary>Screen px of travel before a press on the preview counts as a drag.</summary>
        private const float DragThreshold = 4f;

        /// <summary>Degrees the character turns per screen px dragged, as on the lobby's stage.</summary>
        private const float DegreesPerPixel = 0.4f;

        /// <summary>Marks what the window moves, for CharacterWindow.uss's "Motion" rules.</summary>
        internal const string MotionClass = "charwin-motion";

        /// <summary>UiMotion's entrance variants (ui-controls.uss): from the left, from the right, scaled up.</summary>
        internal const string EnterFromLeft = "ui-enter--left";
        internal const string EnterFromRight = "ui-enter--right";
        internal const string EnterScaled = "ui-enter--scale";

        /// <summary>How long the Save ring takes each way while there is something to save.</summary>
        private const long SavePulseMs = 800;

        private readonly LobbyAssetCatalog _catalog;
        private readonly CharacterRoster _roster;
        private readonly Func<string, Texture> _portraitOf;
        private readonly Func<string, Department> _departmentOf;
        private readonly InputTextEntryScope _textEntry = new();
        private readonly CharacterWindowModel _model = new();
        private readonly Dictionary<string, Button> _tabs = new();
        private readonly Dictionary<string, VisualElement> _panes = new();
        private readonly List<string> _outfitJobs = new();

        private VisualElement _root;
        private VisualElement _window;
        private Action _unfit;
        private CharacterAppearanceTab _appearance;
        private CharacterJobsTab _jobs;
        private CharacterAntagsTab _antags;
        private CharacterSwitcher _switcher;
        private Button _switchButton;
        private VisualElement _switchPortrait;
        private Label _switchName;
        private Label _switchCount;
        private VisualElement _status;
        private Label _statusLabel;
        private Button _discard;
        private Button _save;
        private VisualElement _saveRing;
        private IVisualElementScheduledItem _savePulse;
        private VisualElement _title;
        private VisualElement _tabColumn;
        private VisualElement _continue;
        private VisualElement _previewImage;
        private VisualElement _preview;
        private VisualElement _under;
        private List<Button> _angles;
        private UiSelect _outfit;
        private Label _caption;
        private VisualElement _prompt;
        private VisualElement _promptCard;
        private Label _promptError;
        private Button _promptSave;
        private Action _pending;
        private string _jobsTabDept;
        private Texture _previewTexture;
        private string _activeTab = TabProfile;
        private int _angleIndex;
        private int _outfitIndex;
        private string _fallbackJob = "Assistant";
        private bool _previewPressed;
        private float _previewDragged;

        /// <summary>Return to lobby, after any unsaved changes were saved or discarded.</summary>
        public event Action ReturnRequested;
        /// <summary>Save: the stored character, name normalised. The host puts it in the roster, writes and sends it.</summary>
        public event Action<SavedCharacter> Saved;
        /// <summary>A switcher choice, once unsaved changes are resolved.</summary>
        public event Action<string> SelectRequested;
        public event Action<string> DuplicateRequested;
        public event Action<string> DeleteRequested;
        public event Action NewRequested;
        /// <summary>0 = Front, 1 = Side, 2 = Back.</summary>
        public event Action<int> AngleChanged;
        /// <summary>The player dragged the preview: degrees to turn the character by, on top of the angle.</summary>
        public event Action<float> CharacterTurned;
        /// <summary>The working copy's body sliders changed.</summary>
        public event Action<SavedCharacter> BodyChanged;
        /// <summary>The working copy's hair, facial hair, eyebrows, skin or eyes changed.</summary>
        public event Action<SavedCharacter> LookChanged;
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

        /// <summary>The player's UI scale (the shell PanelSettings' scale); set it before <see cref="Attach"/>.</summary>
        public Func<float> UserScale { get; set; }

        public VisualElement Root => _root;

        public bool IsVisible => _root != null && _root.style.display != DisplayStyle.None;

        public string ActiveTab => _activeTab;

        public CharacterWindowModel Model => _model;

        public CharacterSwitcher Switcher => _switcher;

        public bool PromptOpen => _prompt != null && _prompt.ClassListContains("charwin__prompt--open");

        /// <summary>The character the window edits (the roster's selected one).</summary>
        public string CharacterId => _model.Saved?.Id;

        /// <summary>False while the job outfit preview is set to no clothes.</summary>
        public bool ClothingShown => _outfitIndex != OutfitNaked;

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
            _window = _root.Q("charwin-window");
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
            _switcher.Mount(_window, _switchButton);

            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            layerRoot.Add(_root);
            // The panes and the preview sit at the bible's fixed positions on the 1200 × 675 surface, so a UI scale above
            // 1 would push them off it: the player's scale shrinks the window but never enlarges it.
            _unfit = UiReferenceScale.Fit(_root, _window, () => Mathf.Min(1f, UserScale?.Invoke() ?? 1f));
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
            CharacterTurned = null;
            BodyChanged = null;
            LookChanged = null;
            OutfitChanged = null;
            _textEntry.Exit();
            StopSavePulse();
            _jobsTabDept = null;
            _unfit?.Invoke();
            _unfit = null;
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
                _outfit?.Close();
                ClosePrompt();
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        /// <summary>Opens on <paramref name="tab"/> with the selected character; on Profile, focus starts in the Name field.</summary>
        public void Open(string tab = TabProfile)
        {
            if (_root == null) return;
            Load(_roster.Selected);
            _angleIndex = 0;
            _outfitIndex = OutfitHighJob;
            RefreshPreviewControls();
            SelectTab(tab);
            SetVisible(true);
            PlayOpening();
            AngleChanged?.Invoke(_angleIndex);
            RaisePreview();
            _root.schedule.Execute(() =>
            {
                if (_activeTab == TabProfile) _appearance.NameField?.Focus();
                else _tabs[_activeTab].Focus();
            }).ExecuteLater(1);
        }

        /// <summary>Edits <paramref name="saved"/> from scratch (open, or the roster's selection changed).</summary>
        public void Load(SavedCharacter saved)
        {
            if (_root == null || saved == null) return;
            _model.Load(saved);
            _appearance.Rebuild();
            _jobs.Rebuild();
            _antags.Rebuild();
            RefreshTopBar();
            RefreshPreviewControls();
            if (IsVisible) RaisePreview();
        }

        /// <summary>Repaints the top bar, the outfit preview and the tabs' state from the working copy.</summary>
        public void Refresh()
        {
            if (_root == null) return;
            RefreshTopBar();
            RefreshPreviewControls();
            _appearance.Refresh();
            _jobs.Refresh();
        }

        /// <summary>The roster changed (count, names, portraits): repaint the switcher's button and an open switcher.</summary>
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
            // No job has a playtime requirement yet (effort deviation): only jobs not in the game are locked.
            _jobs.SetJobs(jobs, describe, _ => null, _fallbackJob);
            _jobs.Rebuild();
            string picked = _outfitIndex > OutfitNaked && _outfitIndex - 2 < _outfitJobs.Count ? _outfitJobs[_outfitIndex - 2] : null;
            _outfitJobs.Clear();
            if (jobs != null) _outfitJobs.AddRange(jobs.Select(job => job.Name).Where(name => !string.IsNullOrEmpty(name)));
            if (picked != null) _outfitIndex = _outfitJobs.Contains(picked) ? _outfitJobs.IndexOf(picked) + 2 : OutfitHighJob;
            RefreshPreviewControls();
        }

        public void SetAntagonistDefault(bool optedIn)
        {
            _antags?.SetServerDefault(optedIn);
            RefreshTabMarks();
        }

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
                _outfit?.Close();
                _switcher.ToggleBelowEnd(_switchButton);
                _switchButton.EnableInClassList("charwin__switch--open", _switcher.IsOpen);
            };
            _switchPortrait = _root.Q("charwin-switch-portrait");
            _switchName = _root.Q<Label>("charwin-switch-name");
            _switchCount = _root.Q<Label>("charwin-switch-count");
            _status = _root.Q("charwin-status");
            _statusLabel = _root.Q<Label>("charwin-status-label");
            _discard = _root.Q<Button>("charwin-discard");
            _discard.clicked += Discard;
            _save = _root.Q<Button>("charwin-save");
            _save.clicked += () => Save();
            _saveRing = _root.Q("charwin-save-ring");
            Button returnButton = _root.Q<Button>("charwin-return");
            returnButton.clicked += RequestReturn;
            _continue = returnButton;
            _title = _root.Q(className: "charwin__title");
        }

        private void BindPreview()
        {
            _preview = _root.Q("charwin-preview");
            _previewImage = _root.Q("charwin-preview-image");
            _under = _root.Q(className: "charwin__under");
            _angles = _root.Q("charwin-angles").Query<Button>(className: "charwin__angle").ToList();
            for (int i = 0; i < _angles.Count; i++)
            {
                int index = i;
                _angles[i].clicked += () => SetAngle(index);
            }

            _preview.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.LeftArrow or KeyCode.RightArrow)) return;
                Rotate(e.keyCode == KeyCode.LeftArrow ? -1 : 1);
                e.StopPropagation();
            });
            _preview.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            _preview.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            _preview.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            _preview.RegisterCallback<PointerCaptureOutEvent>(_ => _previewPressed = false);

            // The job outfit preview (Decision 14): the High job's clothes, none, or any job's, on the Profile pane.
            _outfit = new UiSelect { name = "charwin-outfit", tooltip = "The clothes the preview wears. Other players see your job's." };
            _outfit.AddToClassList("charwin__select");
            _outfit.RegisterValueChangedCallback(_ =>
            {
                if (_outfit.Index < 0) return;
                _outfitIndex = _outfit.Index;
                RefreshPreviewControls();
                RaiseOutfit();
            });
            _caption = LobbyUi.Text("", "charwin__note", "charwin__note--caption");
            _caption.name = "charwin-outfit-caption";
            SetPreviewTexture(_previewTexture);
        }

        private void BindTabs()
        {
            _tabColumn = _root.Q("charwin-tabs");
            foreach (string key in TabOrder)
            {
                Button tab = _root.Q<Button>("charwin-tab-" + key);
                string captured = key;
                tab.clicked += () => SelectTab(captured);
                _tabs[key] = tab;
            }

            VisualElement outfit = LobbyUi.Box("charwin__outfit");
            outfit.Add(_outfit);
            outfit.Add(_caption);
            _appearance = new CharacterAppearanceTab(_model, _catalog, _textEntry,
                () => RandomCharacter.RandomName(_catalog != null ? _catalog.CharacterNames : null, Random),
                draft => RandomCharacter.RandomizeLook(draft, RandomCharacter.LookOptions.From(_catalog), Random),
                outfit);
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

            VisualElement column = _root.Q("charwin-column");
            VisualElement wide = _root.Q("charwin-wide");
            AddPane(column, TabProfile, _appearance.Profile);
            AddPane(column, TabBody, _appearance.Body);
            AddPane(column, TabSkin, _appearance.Skin);
            AddPane(column, TabHair, _appearance.Hair);
            AddPane(column, TabBeard, _appearance.Beard);
            AddPane(wide, TabJobs, _jobs.Root);
            AddPane(wide, TabAntags, _antags.Root);
        }

        /// <summary>Every pane stays in the tree, hidden unless its tab is open, so its controls keep their state.</summary>
        private void AddPane(VisualElement host, string key, VisualElement pane)
        {
            pane.AddToClassList("charwin__pane");
            host.Add(pane);
            _panes[key] = pane;
        }

        private static readonly System.Random Random = new();

        private void BindPrompt()
        {
            _prompt = _root.Q("charwin-prompt");
            _promptCard = _prompt.Q(className: "lobby-prompt__card");
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
            if (!_tabs.ContainsKey(key)) key = TabProfile;
            // A pane eases in when it is newly shown: on a switch, or as the window opens.
            bool entering = key != _activeTab || !IsVisible;
            bool wide = Array.IndexOf(WideTabs, key) >= 0;
            bool backFromWide = !wide && _root.ClassListContains("charwin--wide");
            _activeTab = key;
            foreach ((string each, Button tab) in _tabs) tab.EnableInClassList("charwin__tab--active", each == key);
            foreach ((string each, VisualElement pane) in _panes) pane.EnableInClassList("charwin__pane--hidden", each != key);
            _root.EnableInClassList("charwin--wide", wide);
            // The centre column's panes come in from the right, toward the viewport; Jobs and Antags rise from below.
            if (entering && _panes.TryGetValue(key, out VisualElement shown)) Enter(shown, 0, wide ? null : EnterFromRight);
            // Back from Jobs or Antags, the viewport comes back the way it does when the window opens.
            if (backFromWide && IsVisible)
            {
                Enter(_preview, 40, EnterScaled);
                Enter(_under, 90);
            }

            // A focused field that gets hidden gets no FocusOut: let go of text entry and of the focus.
            _textEntry.Exit();
            _outfit?.Close();
            if (_root.panel?.focusController?.focusedElement is VisualElement focused
                && _panes.Values.Any(pane => pane.Contains(focused) && pane.ClassListContains("charwin__pane--hidden")))
            {
                focused.Blur();
            }
        }

        // ---- Save, discard, return ----

        /// <summary>Saves the working copy; false when there is nothing to save or the name is invalid.</summary>
        public bool Save()
        {
            SavedCharacter saved = _model.Commit();
            if (saved == null)
            {
                if (PromptOpen) ShowPromptError();
                return false;
            }

            // The normalised name goes back into the field.
            _appearance.NameField?.SetValueWithoutNotify(saved.Name);
            RefreshTopBar();
            Saved?.Invoke(saved);
            // The save state flashes green as it turns back to Saved.
            UiMotion.Flash(_status, 700);
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
            _outfit?.Close();
            _prompt.AddToClassList("charwin__prompt--open");
            Enter(_prompt);
            Enter(_promptCard, 0, EnterScaled);
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
                    else if (_outfit.IsOpen) _outfit.Close();
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

        private void Rotate(int step) => SetAngle((_angleIndex + step + AngleLabels.Length) % AngleLabels.Length);

        /// <summary>Front, Side or Back; the booth also drops any turn from dragging.</summary>
        private void SetAngle(int index)
        {
            _angleIndex = index;
            RefreshPreviewControls();
            AngleChanged?.Invoke(_angleIndex);
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _previewPressed = true;
            _previewDragged = 0f;
            _preview.CapturePointer(evt.pointerId);
            _preview.Focus();
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (!_previewPressed) return;
            float dx = evt.deltaPosition.x;
            _previewDragged += Mathf.Abs(dx);
            // Drag right turns the character to its left, as if spinning it by the shoulder.
            if (_previewDragged >= DragThreshold && dx != 0f) CharacterTurned?.Invoke(-dx * DegreesPerPixel);
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            _previewPressed = false;
            if (_preview.HasPointerCapture(evt.pointerId)) _preview.ReleasePointer(evt.pointerId);
        }

        private void RefreshPreviewControls()
        {
            if (_angles == null) return;
            for (int i = 0; i < _angles.Count; i++)
            {
                _angles[i].EnableInClassList("charwin__angle--active", i == _angleIndex);
                _angles[i].EnableInClassList("charwin__seg-option--on", i == _angleIndex);
            }

            _preview.tooltip = $"Character preview, {AngleLabels[_angleIndex]} view. Drag to turn it; Q and E, or the arrow keys, turn it to a side.";
            if (_model.Working == null) return;

            string high = _model.Working.Preferences.HighJob;
            List<string> choices = new()
            {
                // SquareDeal has no brackets or colon (Kode Mono fills them in, out of step): a comma reads cleaner.
                "high job, " + (high != null ? high.ToLowerInvariant() : "none set"),
                "naked",
            };
            choices.AddRange(_outfitJobs.Select(job => job.ToLowerInvariant()));
            if (_outfitIndex >= choices.Count) _outfitIndex = OutfitHighJob;
            _outfit.Choices = choices;
            _outfit.SetValueWithoutNotify(choices[_outfitIndex]);
            _caption.text = _outfitIndex > OutfitNaked
                ? $"Wearing {_outfitJobs[_outfitIndex - 2]} clothes, to try them on. Other players see your job's."
                : _model.OutfitCaption(_fallbackJob, _outfitIndex == OutfitHighJob);
        }

        private void RaisePreview()
        {
            BodyChanged?.Invoke(_model.Working);
            LookChanged?.Invoke(_model.Working);
            RaiseOutfit();
        }

        private void RaiseOutfit()
        {
            string job = _outfitIndex > OutfitNaked && _outfitIndex - 2 < _outfitJobs.Count
                ? _outfitJobs[_outfitIndex - 2]
                : _model.OutfitJob(_fallbackJob);
            OutfitChanged?.Invoke(job, ClothingShown);
        }

        // ---- Switcher button and save state ----

        private void RefreshTopBar()
        {
            SavedCharacter working = _model.Working;
            if (working == null) return;
            SavedCharacter saved = _model.Saved;
            _switchName.text = string.IsNullOrWhiteSpace(saved.Name) ? "Unnamed" : saved.Name;
            _switchCount.text = $"{_roster.Count}/{CharacterRoster.MaxCharacters}";
            string high = saved.Preferences?.HighJob;
            _switchButton.tooltip = "Switch character, current " + _switchName.text + (high != null ? ", High: " + high : "");
            Texture portrait = _portraitOf(saved.Id);
            _switchPortrait.style.backgroundImage = portrait switch
            {
                RenderTexture rt => Background.FromRenderTexture(rt),
                Texture2D tex => new StyleBackground(tex),
                _ => StyleKeyword.None,
            };

            bool dirty = _model.IsDirty;
            _status.EnableInClassList("charwin__save--dirty", dirty);
            _statusLabel.text = dirty ? "unsaved changes" : "saved";
            _discard.SetEnabled(dirty);
            bool ready = _model.CanSave;
            _save.SetEnabled(ready);
            _save.tooltip = _model.SaveTooltip;
            // Something valid to save: Save turns into the call to action, its ring breathing until it is pressed.
            _save.EnableInClassList("charwin__save-button--ready", ready);
            if (!ready) StopSavePulse();
            else if (_savePulse == null) _savePulse = UiMotion.Pulse(_saveRing, SavePulseMs);
            RefreshTabMarks();
        }

        private void StopSavePulse()
        {
            if (_savePulse == null) return;
            UiMotion.StopPulse(_saveRing, _savePulse);
            _savePulse = null;
        }

        /// <summary>
        /// What the tab column says before a tab is opened: Jobs takes the High job's department (its accent bar and
        /// its glyph; grey with no High job), and Antags turns red while any role is switched on.
        /// </summary>
        private void RefreshTabMarks()
        {
            if (_model.Working == null || !_tabs.TryGetValue(TabJobs, out Button jobs)) return;
            string high = _model.Working.Preferences.HighJob;
            string dept = high != null ? LobbyView.DepartmentClass(_departmentOf(high)) : null;
            if (dept != _jobsTabDept)
            {
                if (_jobsTabDept != null) jobs.RemoveFromClassList(_jobsTabDept);
                if (dept != null) jobs.AddToClassList(dept);
                jobs.EnableInClassList("charwin__tab--dept", dept != null);
                _jobsTabDept = dept;
            }

            _tabs[TabAntags].EnableInClassList("charwin__tab--armed", _antags != null && _antags.AnyOn);
        }

        // ---- Motion ----

        /// <summary>
        /// The window's entrance: the title from the left, the tabs one after another, the viewport scaling up, then the
        /// bottom row. The open tab's pane comes in with them (<see cref="SelectTab"/>).
        /// </summary>
        private void PlayOpening()
        {
            Enter(_title, 0, EnterFromLeft);
            Stagger(_tabColumn, 30, 30, EnterFromLeft);
            Enter(_preview, 90, EnterScaled);
            Enter(_under, 150);
            Enter(_status, 180);
            Enter(_continue, 210);
        }

        /// <summary>
        /// <see cref="UiMotion.Enter"/> as the window plays it. The window enters again what was shown before (the tab
        /// column on each open, a pane on each tab switch), so CharacterWindow.uss ("Motion") snaps the hidden look on
        /// <see cref="MotionClass"/> rather than easing it, and a stagger's delay is waited out here, on the scheduler,
        /// rather than set as an inline transition delay: that would hold the hidden look back too, and stay on a tab or
        /// a tile and slow every hover after it.
        /// </summary>
        internal static void Enter(VisualElement element, long delayMs = 0, string variant = null)
        {
            if (element == null) return;
            element.AddToClassList(MotionClass);
            if (variant != null) element.AddToClassList(variant);
            element.AddToClassList(UiMotion.EnterClass);
            element.RemoveFromClassList(UiMotion.InClass);
            // Without a panel (EditMode tests, a detached tree) nothing would add it: show the element as it is.
            if (element.panel == null)
            {
                element.AddToClassList(UiMotion.InClass);
                return;
            }

            // A frame first, so the hidden look is drawn before the way in starts.
            element.schedule.Execute(() => element.AddToClassList(UiMotion.InClass)).ExecuteLater(16 + delayMs);
        }

        /// <summary>Enters each child of <paramref name="parent"/>, <paramref name="stepMs"/> apart; after twelve, together.</summary>
        internal static void Stagger(VisualElement parent, long stepMs, long firstMs = 0, string variant = null)
        {
            if (parent == null) return;
            int i = 0;
            foreach (VisualElement child in parent.Children())
            {
                Enter(child, firstMs + (Math.Min(i, 12) * stepMs), variant);
                i++;
            }
        }
    }
}
