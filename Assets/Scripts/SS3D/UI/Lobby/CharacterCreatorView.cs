using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Full-screen Character Creator (Phase B: guided-steps visual + local mock state).
    /// Opened from the lobby preview; Return to Lobby restores the shell.
    /// </summary>
    public sealed class CharacterCreatorView : IUiSurface
    {
        private static readonly System.Text.RegularExpressions.Regex StyleWordBoundary = new("([a-z])([A-Z])");
        private readonly LobbyAssetCatalog _catalog;

        private VisualElement _root;
        private VisualElement _bodyHost;
        private VisualElement _previewImage;
        private Label _angleLabel;
        private Label _loadoutNameLabel;
        private VisualElement _loadoutThumb;
        private VisualElement _loadoutMenu;
        private Texture _livePreviewTexture;

        private int _stepIndex;
        private string _editName = LobbyMockData.CharacterName;
        private string _species = CharacterCreatorMockData.SpeciesOptions[0];
        private bool _saved;
        private readonly CharacterRoster _roster;
        private readonly Func<CharacterCreatorDraft> _createRandom;
        // The edited character's preferences, carried through Save untouched (the lobby's Jobs tab edits them).
        private CharacterPreferences _editPreferences = new();
        private Label _nameError;
        // Booth head crops per character id; owned (and released) by LobbyUiSubSystem.
        private readonly Dictionary<string, Texture> _portraits = new();
        private int _outfitIndex;
        private int _hairColorIndex;
        private int _beardColorIndex;
        private int _eyebrowColorIndex;
        private int _eyeColorIndex;
        private int _skinColorIndex;
        private int _angleIndex;
        private string _styleTab = "hair";
        // Catalog-backed style indices (0 = none).
        private int _hairStyleIndex;
        private int _beardStyleIndex;
        private string _selectedBrows = "thin";
        private string _loadoutId;
        private bool _loadoutOpen;

        private readonly Dictionary<string, float> _sliders = new();
        // Focused name field masks keybinds (B = map editor, Escape = menu).
        private readonly InputTextEntryScope _textEntry = new();

        public event Action ReturnToLobbyRequested;

        public event Action<CharacterCreatorDraft> CharacterSaved;

        public event Action<int> PreviewAngleChanged;
        public event Action<int> OutfitChanged;

        /// <summary>Fired when a body morph slider changes (live preview).</summary>
        public event Action<IReadOnlyDictionary<string, float>> BodyMorphsChanged;

        /// <summary>
        /// Fired when hair style, beard style, or hair colour selection changes.
        /// Args: hairStyleIndex, beardStyleIndex, hairColorIndex, beardColorIndex.
        /// </summary>
        public event Action<int, int, int, int> StyleChanged;
        public event Action<Color, Color, string, Color> FaceChanged;

        /// <param name="roster">The saved characters; Save stores into it and the host writes it to disk.</param>
        /// <param name="createRandom">Makes the unsaved character "+ New Character" starts from (Decision 3).</param>
        public CharacterCreatorView(LobbyAssetCatalog catalog, CharacterRoster roster, Func<CharacterCreatorDraft> createRandom)
        {
            _catalog = catalog;
            _roster = roster;
            _createRandom = createRandom;
            _loadoutId = roster.SelectedId;
            foreach (CharacterCreatorMockData.SliderDef slider in CharacterCreatorMockData.BodySliders)
            {
                _sliders[slider.Key] = slider.DefaultValue;
            }
        }

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement { name = "character-creator-surface" };
            _root.AddToClassList("char-creator");
            _root.pickingMode = PickingMode.Position;

            if (_catalog != null && _catalog.LobbyStyle != null)
            {
                _root.styleSheets.Add(_catalog.LobbyStyle);
            }

            VisualElement window = new() { name = "character-creator-window" };
            window.AddToClassList("char-creator__window");
            window.pickingMode = PickingMode.Position;
            _root.Add(window);

            BuildChrome(window);
            layerRoot.Add(_root);
            Rebuild();
            SetVisible(false);
        }

        public void Detach()
        {
            ReturnToLobbyRequested = null;
            CharacterSaved = null;
            PreviewAngleChanged = null;
            OutfitChanged = null;
            BodyMorphsChanged = null;
            StyleChanged = null;
            FaceChanged = null;
            if (_root != null && _root.parent != null)
            {
                _root.parent.Remove(_root);
            }

            _root = null;
            _bodyHost = null;
            _previewImage = null;
            _angleLabel = null;
            _loadoutNameLabel = null;
            _loadoutThumb = null;
            _loadoutMenu = null;
            _livePreviewTexture = null;
            _textEntry.Exit();
        }

        public void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) _textEntry.Exit();
            if (visible)
            {
                _loadoutOpen = false;
                Rebuild();
            }
        }

        public void Open()
        {
            SetVisible(true);
            PreviewAngleChanged?.Invoke(_angleIndex);
            NotifyBodyMorphsChanged();
            NotifyStyleChanged();
            NotifyFaceChanged();
        }

        public void ApplyDraft(CharacterCreatorDraft draft)
        {
            if (draft == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(draft.Name))
            {
                _editName = draft.Name;
            }

            foreach (KeyValuePair<string, float> pair in draft.BodyMorphs)
            {
                _sliders[pair.Key] = pair.Value;
            }

            _loadoutId = draft.Id;
            _editPreferences = draft.Preferences ?? new CharacterPreferences();
            _outfitIndex = draft.OutfitIndex;
            OutfitChanged?.Invoke(_outfitIndex);
            _hairStyleIndex = draft.HairStyleIndex;
            _beardStyleIndex = draft.BeardStyleIndex;
            _hairColorIndex = draft.HairColorIndex;
            _beardColorIndex = draft.BeardColorIndex;
            _eyebrowColorIndex = draft.EyebrowColorIndex;
            _eyeColorIndex = draft.EyeColorIndex;
            _skinColorIndex = draft.SkinColorIndex;
            _species = "Human";
            _selectedBrows = SS3D.Systems.Entities.Character.CharacterAppearance.NormalizeEyebrowStyle(draft.Eyebrows);
            _saved = false;

            if (_root != null && _root.style.display == DisplayStyle.Flex)
            {
                Rebuild();
            }

            NotifyBodyMorphsChanged();
            NotifyStyleChanged();
            NotifyFaceChanged();
        }

        /// <summary>Current body slider values (0–1) for live booth apply.</summary>
        public IReadOnlyDictionary<string, float> CurrentBodyMorphs => _sliders;

        public void NotifyBodyMorphsChanged()
        {
            _saved = false;
            BodyMorphsChanged?.Invoke(_sliders);
        }

        public void NotifyStyleChanged()
        {
            _saved = false;
            StyleChanged?.Invoke(_hairStyleIndex, _beardStyleIndex, _hairColorIndex, _beardColorIndex);
        }

        private void NotifyFaceChanged()
        {
            FaceChanged?.Invoke(PaletteColor(CharacterCreatorMockData.SkinColors, _skinColorIndex),
                PaletteColor(CharacterCreatorMockData.EyeColors, _eyeColorIndex), _selectedBrows,
                _catalog.GetHairColor(_eyebrowColorIndex));
        }

        public static Color PaletteColor(Color[] palette, int index) => palette[Mathf.Clamp(index, 0, palette.Length - 1)];

        /// <summary>Id of the character currently being edited.</summary>
        public string CurrentCharacterId => _loadoutId;

        public IEnumerable<CharacterCreatorDraft> SavedCharacters => _roster.Characters;

        /// <summary>Shows a rendered portrait for the character in the selector and its menu row.</summary>
        public void SetPortrait(string characterId, Texture portrait)
        {
            if (string.IsNullOrEmpty(characterId) || portrait == null)
            {
                return;
            }

            _portraits[characterId] = portrait;
            if (characterId == _loadoutId)
            {
                ApplyPortrait(_loadoutThumb, characterId);
            }
        }

        public void SetPreviewTexture(Texture texture)
        {
            _livePreviewTexture = texture;
            RefreshPreview();
        }

        private void BuildChrome(VisualElement window)
        {
            VisualElement top = new();
            top.AddToClassList("char-creator__top");

            Label title = new("CHARACTER CREATOR");
            title.AddToClassList("char-creator__title");
            title.AddToClassList("font-titling");
            top.Add(title);

            VisualElement topRight = new();
            topRight.AddToClassList("char-creator__top-right");

            VisualElement loadoutWrap = new();
            loadoutWrap.AddToClassList("char-creator__loadout-wrap");

            VisualElement loadoutBtn = new();
            loadoutBtn.AddToClassList("char-creator__loadout-btn");
            loadoutBtn.pickingMode = PickingMode.Position;
            loadoutBtn.RegisterCallback<ClickEvent>(_ =>
            {
                _loadoutOpen = !_loadoutOpen;
                Rebuild();
            });

            _loadoutThumb = new VisualElement();
            _loadoutThumb.AddToClassList("char-creator__loadout-thumb");
            _loadoutThumb.pickingMode = PickingMode.Ignore;
            loadoutBtn.Add(_loadoutThumb);

            _loadoutNameLabel = new();
            _loadoutNameLabel.AddToClassList("char-creator__loadout-name");
            _loadoutNameLabel.AddToClassList("font-body");
            _loadoutNameLabel.pickingMode = PickingMode.Ignore;
            loadoutBtn.Add(_loadoutNameLabel);

            VisualElement chevron = new();
            chevron.AddToClassList("char-creator__loadout-chevron");
            chevron.pickingMode = PickingMode.Ignore;
            if (_catalog != null && _catalog.ChevronDown != null)
            {
                chevron.style.backgroundImage = new StyleBackground(_catalog.ChevronDown);
            }

            loadoutBtn.Add(chevron);
            loadoutWrap.Add(loadoutBtn);

            _loadoutMenu = new VisualElement();
            _loadoutMenu.AddToClassList("char-creator__loadout-menu");
            topRight.Add(loadoutWrap);

            Button returnBtn = new(() => ReturnToLobbyRequested?.Invoke())
            {
                text = "RETURN TO LOBBY",
            };
            returnBtn.AddToClassList("char-creator__return");
            returnBtn.AddToClassList("font-titling");
            topRight.Add(returnBtn);

            top.Add(topRight);
            window.Add(top);

            VisualElement main = new();
            main.AddToClassList("char-creator__main");
            window.Add(main);

            VisualElement rail = new();
            rail.AddToClassList("char-creator__rail");
            rail.name = "char-creator-rail";
            main.Add(rail);

            _bodyHost = new VisualElement();
            _bodyHost.AddToClassList("char-creator__body");
            main.Add(_bodyHost);

            VisualElement previewDock = new();
            previewDock.AddToClassList("char-creator__preview-dock");

            VisualElement previewFrame = new();
            previewFrame.AddToClassList("char-creator__preview-frame");
            _previewImage = new VisualElement();
            _previewImage.AddToClassList("char-creator__preview-image");
            previewFrame.Add(_previewImage);
            previewDock.Add(previewFrame);

            VisualElement angleRow = new();
            angleRow.AddToClassList("char-creator__angle-row");
            Button prevAngle = new(() =>
            {
                _angleIndex = (_angleIndex + CharacterCreatorMockData.AngleLabels.Length - 1)
                    % CharacterCreatorMockData.AngleLabels.Length;
                PreviewAngleChanged?.Invoke(_angleIndex);
                RefreshPreview();
            })
            {
                text = "‹",
            };
            prevAngle.AddToClassList("char-creator__angle-btn");
            prevAngle.AddToClassList("font-body");
            angleRow.Add(prevAngle);

            _angleLabel = new();
            _angleLabel.AddToClassList("char-creator__angle-label");
            _angleLabel.AddToClassList("font-terminal");
            angleRow.Add(_angleLabel);

            Button nextAngle = new(() =>
            {
                _angleIndex = (_angleIndex + 1) % CharacterCreatorMockData.AngleLabels.Length;
                PreviewAngleChanged?.Invoke(_angleIndex);
                RefreshPreview();
            })
            {
                text = "›",
            };
            nextAngle.AddToClassList("char-creator__angle-btn");
            nextAngle.AddToClassList("font-body");
            angleRow.Add(nextAngle);
            previewDock.Add(angleRow);

            main.Add(previewDock);
            // Absolute popup is last in paint order so the step form cannot cover it.
            window.Add(_loadoutMenu);
        }

        private void Rebuild()
        {
            if (_root == null || _bodyHost == null)
            {
                return;
            }

            // A focused field removed by the rebuild gets no FocusOut.
            _textEntry.Exit();
            RebuildRail();
            RebuildLoadoutChrome();
            _bodyHost.Clear();
            ScrollView content = new(ScrollViewMode.Vertical);
            content.AddToClassList("char-creator__content-scroll");
            BuildStepContent(content);
            _bodyHost.Add(content);
            BuildStepNav(_bodyHost);
            RefreshPreview();
        }

        private void RebuildRail()
        {
            VisualElement rail = _root.Q("char-creator-rail");
            if (rail == null)
            {
                return;
            }

            rail.Clear();
            for (int i = 0; i < CharacterCreatorMockData.Steps.Length; i++)
            {
                CharacterCreatorMockData.StepDef step = CharacterCreatorMockData.Steps[i];
                bool active = i == _stepIndex;
                int index = i;

                VisualElement row = new();
                row.AddToClassList("char-creator__rail-item");
                row.EnableInClassList("char-creator__rail-item--active", active);
                row.pickingMode = PickingMode.Position;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _stepIndex = index;
                    Rebuild();
                });

                Label num = new((i + 1).ToString("00"));
                num.AddToClassList("char-creator__rail-num");
                num.AddToClassList("font-terminal");
                num.pickingMode = PickingMode.Ignore;
                row.Add(num);

                Label label = new(step.Label.ToUpperInvariant());
                label.AddToClassList("char-creator__rail-label");
                label.AddToClassList("font-titling");
                label.pickingMode = PickingMode.Ignore;
                row.Add(label);
                rail.Add(row);
            }
        }

        private void RebuildLoadoutChrome()
        {
            if (_loadoutNameLabel != null)
            {
                _loadoutNameLabel.text = _roster.Find(_loadoutId)?.Name ?? _editName;
            }

            ApplyPortrait(_loadoutThumb, _loadoutId);

            if (_loadoutMenu == null)
            {
                return;
            }

            _loadoutMenu.Clear();
            _loadoutMenu.style.display = _loadoutOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_loadoutOpen)
            {
                return;
            }

            foreach (CharacterCreatorDraft saved in _roster.Characters)
            {
                string id = saved.Id;
                VisualElement row = new();
                row.AddToClassList("char-creator__loadout-row");
                row.EnableInClassList("char-creator__loadout-row--active", id == _loadoutId);
                row.pickingMode = PickingMode.Position;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _loadoutId = id;
                    ApplyDraft(saved);
                    _angleIndex = 0;
                    _stepIndex = 0;
                    PreviewAngleChanged?.Invoke(_angleIndex);
                    _loadoutOpen = false;
                    Rebuild();
                });

                VisualElement thumb = new();
                thumb.AddToClassList("char-creator__loadout-row-thumb");
                thumb.pickingMode = PickingMode.Ignore;
                ApplyPortrait(thumb, id);
                row.Add(thumb);

                Label name = new(saved.Name);
                name.AddToClassList("char-creator__loadout-row-name");
                name.AddToClassList("font-body");
                name.pickingMode = PickingMode.Ignore;
                row.Add(name);
                _loadoutMenu.Add(row);
            }

            VisualElement newRow = new();
            newRow.AddToClassList("char-creator__loadout-new");
            Label newLabel = new("+ NEW CHARACTER");
            newLabel.AddToClassList("char-creator__loadout-new-label");
            newLabel.AddToClassList("font-body");
            newRow.Add(newLabel);
            newRow.pickingMode = PickingMode.Position;
            // Up to CharacterRoster.MaxCharacters; a full roster hides New.
            newRow.style.display = _roster.CanAdd ? DisplayStyle.Flex : DisplayStyle.None;
            newRow.RegisterCallback<ClickEvent>(_ =>
            {
                CharacterCreatorDraft random = _createRandom();
                _loadoutId = random.Id;
                ApplyDraft(random);
                _angleIndex = 0;
                PreviewAngleChanged?.Invoke(_angleIndex);
                _loadoutOpen = false;
                _stepIndex = 0;
                Rebuild();
            });
            _loadoutMenu.Add(newRow);
        }

        private void BuildStepContent(VisualElement host)
        {
            string key = CharacterCreatorMockData.Steps[_stepIndex].Key;
            switch (key)
            {
                case "identity":
                {
                    BuildIdentity(host);
                    break;
                }

                case "body":
                {
                    BuildBody(host);
                    break;
                }

                case "style":
                {
                    BuildStyle(host);
                    break;
                }

                default:
                {
                    BuildReview(host);
                    break;
                }
            }
        }

        private void BuildIdentity(VisualElement host)
        {
            VisualElement panel = new();
            panel.AddToClassList("char-creator__form");

            panel.Add(FieldLabel("Character Name"));
            TextField nameField = new() { value = _editName };
            nameField.AddToClassList("char-creator__input");
            nameField.AddToClassList("font-body");
            nameField.label = string.Empty;
            nameField.name = "character-name";
            nameField.RegisterValueChangedCallback(evt =>
            {
                _editName = evt.newValue ?? string.Empty;
                _saved = false;
                ShowNameError();
            });
            StyleDarkTextField(nameField);
            nameField.RegisterCallback<FocusInEvent>(_ => _textEntry.Enter());
            nameField.RegisterCallback<FocusOutEvent>(_ => _textEntry.Exit());
            panel.Add(nameField);
            // Interim error line until the Phase 5 window; the rules are shared with the server (Decision 4).
            _nameError = new Label { name = "character-name-error" };
            _nameError.AddToClassList("font-body");
            _nameError.style.color = new Color(1f, 0.55f, 0.5f);
            _nameError.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(_nameError);
            ShowNameError();

            panel.Add(FieldLabel("Species"));
            DropdownField species = new(new List<string> { "Human" }, 0)
            {
                name = "character-species",
                tooltip = "Only Human is available. Other species need their own models.",
            };
            species.AddToClassList("char-creator__input");
            species.AddToClassList("font-body");
            // DropdownField's menu factory is internal in Unity 6; intercept opening to expose
            // unavailable species as disabled entries while keeping the field's choices Human-only.
            void ShowSpeciesMenu()
            {
                GenericDropdownMenu menu = new();
                menu.AddItem("Human", true, () => species.value = "Human");
                foreach (string option in CharacterCreatorMockData.SpeciesOptions)
                    if (option != "Human") menu.AddDisabledItem(option + " (unavailable)", false);
                menu.DropDown(species.worldBound, species, true);
            }
            species.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                evt.StopImmediatePropagation();
                ShowSpeciesMenu();
            }, TrickleDown.TrickleDown);
            species.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                ShowSpeciesMenu();
            }, TrickleDown.TrickleDown);
            species.RegisterValueChangedCallback(_ => species.SetValueWithoutNotify("Human"));
            panel.Add(species);

            host.Add(panel);
        }

        private void BuildBody(VisualElement host)
        {
            VisualElement panel = new();
            panel.AddToClassList("char-creator__form");
            panel.AddToClassList("char-creator__form--wide");

            Label section = new("BODY & SKIN");
            section.AddToClassList("char-creator__section");
            section.AddToClassList("font-titling");
            panel.Add(section);

            foreach (CharacterCreatorMockData.SliderDef def in CharacterCreatorMockData.BodySliders)
            {
                panel.Add(BuildSliderRow(def));
            }

            panel.Add(FieldLabel("Skin Tone"));
            VisualElement skinColors = BuildColorRow(CharacterCreatorMockData.SkinColors, _skinColorIndex, index =>
            {
                _skinColorIndex = index;
                _saved = false;
                NotifyFaceChanged();
                Rebuild();
            });
            skinColors.name = "skin-color-picker";
            panel.Add(skinColors);
            panel.Add(FieldLabel("Eye Color"));
            VisualElement eyeColors = BuildColorRow(
                CharacterCreatorMockData.EyeColors,
                _eyeColorIndex,
                index =>
                {
                    _eyeColorIndex = index;
                    _saved = false;
                    NotifyFaceChanged();
                    Rebuild();
                });
            eyeColors.name = "eye-color-picker";
            panel.Add(eyeColors);

            host.Add(panel);
        }

        private VisualElement BuildSliderRow(CharacterCreatorMockData.SliderDef def)
        {
            VisualElement row = new();
            row.AddToClassList("char-creator__slider-row");

            Label label = new(def.Label);
            label.AddToClassList("char-creator__slider-label");
            label.AddToClassList("font-body");
            row.Add(label);

            float value = _sliders.TryGetValue(def.Key, out float stored) ? stored : def.DefaultValue;
            Label valueLabel = new(value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            valueLabel.AddToClassList("char-creator__slider-value");
            valueLabel.AddToClassList("font-terminal");
            row.Add(valueLabel);

            Slider slider = new(0f, 1f, SliderDirection.Horizontal, 0f)
            {
                name = "body-" + def.Key,
                value = value,
                showInputField = false,
            };
            slider.AddToClassList("char-creator__slider");
            VisualElement fill = new() { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("char-creator__slider-fill");
            fill.style.width = Length.Percent(value * 100f);
            slider.Q(className: "unity-base-slider__tracker").Add(fill);
            string key = def.Key;
            slider.RegisterValueChangedCallback(evt =>
            {
                _sliders[key] = evt.newValue;
                fill.style.width = Length.Percent(evt.newValue * 100f);
                valueLabel.text = evt.newValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                NotifyBodyMorphsChanged();
            });
            row.Add(slider);
            return row;
        }

        private void BuildStyle(VisualElement host)
        {
            VisualElement panel = new();
            panel.AddToClassList("char-creator__form");
            panel.AddToClassList("char-creator__form--wide");

            VisualElement tabs = new();
            tabs.AddToClassList("char-creator__style-tabs");
            foreach (CharacterCreatorMockData.StyleTabDef tab in CharacterCreatorMockData.StyleTabs)
            {
                string key = tab.Key;
                Button btn = new(() =>
                {
                    _styleTab = key;
                    Rebuild();
                })
                {
                    text = tab.Label.ToUpperInvariant(),
                };
                btn.AddToClassList("char-creator__style-tab");
                btn.AddToClassList("font-titling");
                btn.EnableInClassList("char-creator__style-tab--active", _styleTab == key);
                tabs.Add(btn);
            }

            panel.Add(tabs);

            ScrollView gridScroll = new();
            gridScroll.AddToClassList("char-creator__style-scroll");
            VisualElement grid = new();
            grid.AddToClassList("char-creator__style-grid");

            switch (_styleTab)
            {
                case "facialHair":
                {
                    BuildBeardTiles(grid);
                    break;
                }

                case "eyebrows":
                {
                    grid.Add(BuildStyleTile("None", true, _selectedBrows == "none", () => SelectEyebrows("none")));
                    foreach (LobbyNamedPrefab style in _catalog.EyebrowStyles)
                    {
                        string id = style.Id;
                        VisualElement tile = BuildStyleTile(StyleDisplayName(id).Replace("HumanEyebrows", ""), false,
                            _selectedBrows == id, () => SelectEyebrows(id));
                        tile.name = "eyebrows-" + id;
                        grid.Add(tile);
                    }
                    break;
                }

                default:
                {
                    BuildHairTiles(grid);
                    break;
                }
            }

            gridScroll.Add(grid);
            panel.Add(gridScroll);
            BuildStyleColorPicker(panel);
            host.Add(panel);
        }

        private void SelectEyebrows(string id)
        {
            _selectedBrows = id;
            _saved = false;
            NotifyFaceChanged();
            Rebuild();
        }

        private void BuildStyleColorPicker(VisualElement panel)
        {
            string label;
            int selectedIndex;
            switch (_styleTab)
            {
                case "facialHair":
                {
                    label = "Facial Hair Color";
                    selectedIndex = _beardColorIndex;
                    break;
                }
                case "eyebrows":
                {
                    label = "Eyebrow Color";
                    selectedIndex = _eyebrowColorIndex;
                    break;
                }
                default:
                {
                    label = "Hair Color";
                    selectedIndex = _hairColorIndex;
                    break;
                }
            }

            VisualElement picker = new() { name = "style-color-picker" };
            picker.AddToClassList("char-creator__style-colors");
            picker.Add(FieldLabel(label));
            Color[] colors = _catalog != null && _catalog.HairColors != null && _catalog.HairColors.Count > 0
                ? _catalog.HairColors.ToArray()
                : CharacterCreatorMockData.HairColors;
            picker.Add(BuildColorRow(colors, selectedIndex, index =>
            {
                switch (_styleTab)
                {
                    case "facialHair":
                    {
                        _beardColorIndex = index;
                        break;
                    }
                    case "eyebrows":
                    {
                        _eyebrowColorIndex = index;
                        NotifyFaceChanged();
                        break;
                    }
                    default:
                    {
                        _hairColorIndex = index;
                        break;
                    }
                }
                NotifyStyleChanged();
                Rebuild();
            }));
            panel.Add(picker);
        }

        private void BuildHairTiles(VisualElement grid)
        {
            bool hasCatalog = _catalog != null && _catalog.HairStyles != null && _catalog.HairStyles.Count > 0;
            if (!hasCatalog)
            {
                // Hair prefabs not yet in catalog — run SS3D → Data → Rebuild All UI Catalogs.
                Debug.LogWarning("[CharacterCreator] HairStyles list is empty. Run SS3D → Data → Rebuild All UI Catalogs.");
                BuildMockStyleTiles(grid, CharacterCreatorMockData.StyleOptionsFor("hair"), "none",
                    _ =>
                    {
                        _hairStyleIndex = 0;
                        NotifyStyleChanged();
                        Rebuild();
                    });
                return;
            }

            for (int i = 0; i < _catalog.HairStyles.Count; i++)
            {
                int index = i;
                LobbyNamedPrefab entry = _catalog.HairStyles[i];
                bool isNone = i == 0;
                string label = isNone ? "None" : StyleDisplayName(entry.Id);
                VisualElement tile = BuildStyleTile(label, isNone, _hairStyleIndex == index, () =>
                {
                    _hairStyleIndex = index;
                    NotifyStyleChanged();
                    Rebuild();
                });
                grid.Add(tile);
            }
        }

        private void BuildBeardTiles(VisualElement grid)
        {
            bool hasCatalog = _catalog != null && _catalog.BeardStyles != null && _catalog.BeardStyles.Count > 0;
            if (!hasCatalog)
            {
                BuildMockStyleTiles(grid, CharacterCreatorMockData.StyleOptionsFor("facialHair"), "none",
                    _ =>
                    {
                        _beardStyleIndex = 0;
                        NotifyStyleChanged();
                        Rebuild();
                    });
                return;
            }

            for (int i = 0; i < _catalog.BeardStyles.Count; i++)
            {
                int index = i;
                LobbyNamedPrefab entry = _catalog.BeardStyles[i];
                bool isNone = i == 0;
                string label = isNone ? "None" : StyleDisplayName(entry.Id);
                VisualElement tile = BuildStyleTile(label, isNone, _beardStyleIndex == index, () =>
                {
                    _beardStyleIndex = index;
                    NotifyStyleChanged();
                    Rebuild();
                });
                grid.Add(tile);
            }
        }

        private static void BuildMockStyleTiles(
            VisualElement grid,
            System.Collections.Generic.IReadOnlyList<CharacterCreatorMockData.StyleOptionDef> options,
            string selectedKey,
            Action<string> onSelect)
        {
            foreach (CharacterCreatorMockData.StyleOptionDef opt in options)
            {
                string key = opt.Key;
                VisualElement tile = BuildStyleTile(opt.Label, opt.IsNone, key == selectedKey, () => onSelect(key));
                grid.Add(tile);
            }
        }

        private static VisualElement BuildStyleTile(string label, bool isNone, bool isSelected, Action onClick)
        {
            VisualElement tile = new();
            tile.AddToClassList("char-creator__style-tile");
            tile.EnableInClassList("char-creator__style-tile--selected", isSelected);
            tile.pickingMode = PickingMode.Position;
            tile.RegisterCallback<ClickEvent>(_ => onClick());

            if (isNone)
            {
                Label noneMark = new("×");
                noneMark.AddToClassList("char-creator__style-none");
                noneMark.AddToClassList("font-body");
                noneMark.pickingMode = PickingMode.Ignore;
                tile.Add(noneMark);
            }

            Label tileLabel = new(label.ToUpperInvariant());
            tileLabel.AddToClassList("char-creator__style-tile-label");
            tileLabel.AddToClassList("font-body");
            tileLabel.pickingMode = PickingMode.Ignore;
            tile.Add(tileLabel);
            return tile;
        }

        private void BuildReview(VisualElement host)
        {
            VisualElement panel = new();
            panel.AddToClassList("char-creator__form");

            Label heading = new("READY TO DEPLOY");
            heading.AddToClassList("char-creator__review-title");
            heading.AddToClassList("font-titling");
            panel.Add(heading);

            VisualElement summary = new();
            summary.AddToClassList("char-creator__review-card");
            summary.Add(ReviewLine("Name", _editName));
            summary.Add(ReviewLine("Species", _species));
            string hairLabel;
            if (_catalog != null
                && _catalog.HairStyles != null
                && _hairStyleIndex > 0
                && _hairStyleIndex < _catalog.HairStyles.Count)
            {
                hairLabel = StyleDisplayName(_catalog.HairStyles[_hairStyleIndex].Id);
            }
            else
            {
                hairLabel = _hairStyleIndex == 0 ? "None" : "?";
            }
            summary.Add(ReviewLine("Hair", hairLabel));
            panel.Add(summary);

            host.Add(panel);
        }

        private static VisualElement ReviewLine(string label, string value)
        {
            VisualElement row = new();
            row.AddToClassList("char-creator__review-line");
            Label text = new($"{label}: {value}");
            text.AddToClassList("char-creator__review-text");
            text.AddToClassList("font-body");
            row.Add(text);
            return row;
        }

        private void BuildStepNav(VisualElement host)
        {
            VisualElement nav = new();
            nav.AddToClassList("char-creator__nav");

            bool first = _stepIndex <= 0;
            bool last = _stepIndex >= CharacterCreatorMockData.Steps.Length - 1;

            Button back = new(() =>
            {
                if (_stepIndex > 0)
                {
                    _stepIndex--;
                    Rebuild();
                }
            })
            {
                text = "BACK",
            };
            back.AddToClassList("char-creator__nav-btn");
            back.AddToClassList("font-titling");
            back.SetEnabled(!first);
            nav.Add(back);

            string nextLabel = "NEXT";
            if (last) nextLabel = _saved ? "SAVED" : "SAVE CHARACTER";
            Button next = new(() =>
            {
                if (last)
                {
                    SaveCharacter();
                    Rebuild();
                }
                else
                {
                    _stepIndex++;
                    Rebuild();
                }
            })
            {
                text = nextLabel,
            };
            next.AddToClassList("char-creator__nav-btn");
            next.AddToClassList("char-creator__nav-btn--primary");
            next.AddToClassList("font-titling");
            next.EnableInClassList("char-creator__nav-btn--save", last);
            next.SetEnabled(!last || !_saved);
            nav.Add(next);

            host.Add(nav);
        }

        private VisualElement BuildColorRow(Color[] colors, int selectedIndex, Action<int> onSelect)
        {
            VisualElement row = new();
            row.AddToClassList("char-creator__swatch-row");
            for (int i = 0; i < colors.Length; i++)
            {
                int index = i;
                VisualElement swatch = new();
                swatch.AddToClassList("char-creator__swatch");
                swatch.AddToClassList("char-creator__swatch--small");
                swatch.EnableInClassList("char-creator__swatch--selected", selectedIndex == index);
                swatch.style.backgroundColor = colors[i];
                swatch.pickingMode = PickingMode.Position;
                swatch.RegisterCallback<ClickEvent>(_ => onSelect(index));
                row.Add(swatch);
            }

            return row;
        }

        private static Label FieldLabel(string text)
        {
            Label label = new(text.ToUpperInvariant());
            label.AddToClassList("char-creator__field-label");
            label.AddToClassList("font-body");
            return label;
        }

        private void RefreshPreview()
        {
            if (_angleLabel != null)
            {
                _angleLabel.text = CharacterCreatorMockData.AngleLabels[_angleIndex];
            }

            if (_previewImage == null)
            {
                return;
            }

            _previewImage.style.scale = new Scale(Vector3.one);

            if (_livePreviewTexture is RenderTexture renderTexture)
            {
                _previewImage.style.backgroundImage = Background.FromRenderTexture(renderTexture);
                return;
            }

            if (_livePreviewTexture is Texture2D texture2D)
            {
                _previewImage.style.backgroundImage = new StyleBackground(texture2D);
                return;
            }

            CharacterCreatorMockData.LoadoutDef loadout = SelectedLoadout();
            ApplyThumb(_previewImage, loadout.ThumbId);
        }

        private void ShowNameError()
        {
            if (_nameError == null) return;
            string text = CharacterNameRules.Describe(CharacterNameRules.Validate(_editName));
            _nameError.text = text;
            _nameError.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Saves the edited character into the roster. An invalid name, or a full roster for a new one, refuses it.</summary>
        private void SaveCharacter()
        {
            if (!CharacterNameRules.TryNormalize(_editName, out string name, out _))
            {
                _stepIndex = 0;
                return;
            }

            CharacterCreatorDraft draft = new()
            {
                Id = _loadoutId,
                Name = name,
                Preferences = _editPreferences.Clone(),
                HairStyleIndex = _hairStyleIndex,
                BeardStyleIndex = _beardStyleIndex,
                OutfitIndex = _outfitIndex,
                HairColorIndex = _hairColorIndex,
                BeardColorIndex = _beardColorIndex,
                EyebrowColorIndex = _eyebrowColorIndex,
                EyeColorIndex = _eyeColorIndex,
                SkinColorIndex = _skinColorIndex,
                Species = _species,
                Eyebrows = _selectedBrows,
            };
            draft.CopyMorphsFrom(_sliders);
            if (!_roster.Replace(draft) && !_roster.Add(draft))
            {
                return;
            }

            _loadoutId = draft.Id;

            _editName = draft.Name;
            _editPreferences = draft.Preferences;
            _saved = true;
            CharacterSaved?.Invoke(draft);
        }

        private static string StyleDisplayName(string id)
        {
            string name = id ?? string.Empty;
            foreach (string prefix in new string[] { "HumanHair", "HumanBeard", "HumanFacialHair", "HumanEyebrows" })
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(prefix.Length);
            name = StyleWordBoundary.Replace(name, "$1 $2");
            return name.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        private static string SelectedLoadoutThumb(string id)
        {
            foreach (CharacterCreatorMockData.LoadoutDef loadout in CharacterCreatorMockData.Loadouts)
                if (loadout.Id == id) return loadout.ThumbId;
            return CharacterCreatorMockData.Loadouts[0].ThumbId;
        }

        private CharacterCreatorMockData.LoadoutDef SelectedLoadout()
        {
            foreach (CharacterCreatorMockData.LoadoutDef loadout in CharacterCreatorMockData.Loadouts)
            {
                if (loadout.Id == _loadoutId)
                {
                    return loadout;
                }
            }

            return CharacterCreatorMockData.Loadouts[0];
        }


        private void ApplyPortrait(VisualElement target, string characterId)
        {
            if (target == null)
            {
                return;
            }

            if (_portraits.TryGetValue(characterId, out Texture portrait) && portrait != null)
            {
                target.style.backgroundImage = portrait is RenderTexture renderTexture
                    ? Background.FromRenderTexture(renderTexture)
                    : new StyleBackground(portrait as Texture2D);
                return;
            }

            ApplyThumb(target, SelectedLoadoutThumb(characterId));
        }

        private void ApplyThumb(VisualElement target, string thumbId)
        {
            if (target == null)
            {
                return;
            }

            Sprite sprite = null;
            if (_catalog != null)
            {
                if (!_catalog.TryGetLoadoutThumb(thumbId, out sprite))
                {
                    sprite = _catalog.PreviewPlaceholder;
                }
            }

            target.style.backgroundImage = sprite != null
                ? new StyleBackground(sprite)
                : StyleKeyword.None;
        }

        private static void StyleDarkTextField(TextField field)
        {
            if (field == null)
            {
                return;
            }

            Color fill = new(0.1647059f, 0.1647059f, 0.1803922f, 1f);
            field.style.backgroundColor = fill;

            void Apply()
            {
                VisualElement input = field.Q(className: "unity-base-text-field__input")
                    ?? field.Q(className: "unity-text-field__input")
                    ?? field.Q(className: "unity-base-field__input");
                if (input != null)
                {
                    input.style.backgroundColor = fill;
                    input.style.borderTopWidth = 0;
                    input.style.borderRightWidth = 0;
                    input.style.borderBottomWidth = 0;
                    input.style.borderLeftWidth = 0;
                }
            }

            Apply();
            field.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }
    }
}
