using System;
using System.Collections.Generic;
using System.Globalization;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>What an Appearance edit changed, so the host updates only that part of the preview.</summary>
    public enum CharacterLookChange
    {
        Name = 0,
        Body = 1,
        Look = 2,
    }

    /// <summary>
    /// The character window's Appearance tab (lobby.md §6, lobby effort Phase 5): the name with Randomize name and its
    /// error line; six body sliders; skin and eye swatches; Hair, Facial hair and Eyebrows grids, each with a colour
    /// row; and Randomize appearance. It edits <see cref="CharacterWindowModel.Working"/>.
    /// </summary>
    public sealed class CharacterAppearanceTab
    {
        private static readonly System.Text.RegularExpressions.Regex WordBoundary = new("([a-z])([A-Z0-9])");

        private readonly CharacterWindowModel _model;
        private readonly LobbyAssetCatalog _catalog;
        private readonly InputTextEntryScope _textEntry;
        private readonly Func<string> _randomName;
        private readonly Action<SavedCharacter> _randomizeLook;
        private readonly ScrollView _scroll;
        private TextField _name;
        private VisualElement _error;
        private Label _errorText;

        public event Action<CharacterLookChange> Changed;

        /// <param name="randomName">A new random name (Randomize name).</param>
        /// <param name="randomizeLook">Re-rolls the look of the draft it's given (Randomize appearance).</param>
        public CharacterAppearanceTab(CharacterWindowModel model, LobbyAssetCatalog catalog, InputTextEntryScope textEntry,
            Func<string> randomName, Action<SavedCharacter> randomizeLook)
        {
            _model = model;
            _catalog = catalog;
            _textEntry = textEntry;
            _randomName = randomName;
            _randomizeLook = randomizeLook;
            Root = LobbyUi.Box("charwin-tab");
            Root.name = "charwin-appearance";
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("charwin-tab__scroll");
            Root.Add(_scroll);
        }

        public VisualElement Root { get; }

        /// <summary>The name field; the window focuses it when it opens.</summary>
        public TextField NameField => _name;

        /// <summary>Rebuilds every control from the working copy (a load, a discard, a randomize).</summary>
        public void Rebuild()
        {
            // A focused field removed by the rebuild gets no FocusOut.
            _textEntry.Exit();
            _scroll.Clear();
            SavedCharacter draft = _model.Working;
            if (draft == null) return;

            VisualElement top = LobbyUi.Box("charwin-look__top");
            top.Add(LobbyUi.Text("Everything here is visible to other players.", "charwin-tab__intro"));
            top.Add(ActionButton("charwin-randomize-look", "sparkles", "Randomize appearance", () =>
            {
                _randomizeLook(_model.Working);
                Rebuild();
                Changed?.Invoke(CharacterLookChange.Body);
                Changed?.Invoke(CharacterLookChange.Look);
            }));
            _scroll.Add(top);

            VisualElement columns = LobbyUi.Box("charwin-look__columns");
            VisualElement left = LobbyUi.Box("charwin-look__left");
            VisualElement right = LobbyUi.Box("charwin-look__right");
            columns.Add(left);
            columns.Add(right);
            _scroll.Add(columns);

            left.Add(BuildName(draft));
            left.Add(BuildBody(draft));
            right.Add(BuildStyle(draft));
            ShowNameError();
        }

        // ---- Name ----

        private VisualElement BuildName(SavedCharacter draft)
        {
            VisualElement section = LobbyUi.Box("charwin-look__section");
            section.Add(LobbyUi.Text("NAME", "charwin-tab__eyebrow"));
            section.Add(LobbyUi.Text("Name", "charwin-tab__label"));
            VisualElement row = LobbyUi.Box("charwin-look__name-row");
            _name = new TextField { name = "charwin-name", value = draft.Name, maxLength = 40 };
            _name.AddToClassList("charwin-look__name");
            _name.textEdition.placeholder = "First Last";
            _name.RegisterValueChangedCallback(e =>
            {
                _model.Working.Name = e.newValue ?? string.Empty;
                ShowNameError();
                Changed?.Invoke(CharacterLookChange.Name);
            });
            LobbyUi.HoldTextEntry(_name, _textEntry);
            LobbyUi.DarkInput(_name);
            row.Add(_name);
            row.Add(ActionButton("charwin-randomize-name", "sparkles", "Randomize name", () => _name.value = _randomName()));
            section.Add(row);

            _error = LobbyUi.Box("charwin-look__error");
            _error.name = "charwin-name-error";
            _error.Add(LobbyUi.Icon("warning"));
            _errorText = LobbyUi.Text("", "charwin-look__error-text");
            _error.Add(_errorText);
            section.Add(_error);
            return section;
        }

        /// <summary>Repaints the name's error line (the only state not rebuilt per edit).</summary>
        public void Refresh() => ShowNameError();

        private void ShowNameError()
        {
            if (_error == null) return;
            string text = CharacterNameRules.Describe(_model.NameError);
            _errorText.text = text;
            _error.EnableInClassList("charwin-look__error--hidden", text.Length == 0);
            _name.EnableInClassList("charwin-look__name--error", text.Length > 0);
        }

        // ---- Body ----

        private VisualElement BuildBody(SavedCharacter draft)
        {
            VisualElement section = LobbyUi.Box("charwin-look__section");
            section.Add(LobbyUi.Text("BODY", "charwin-tab__eyebrow"));
            VisualElement grid = LobbyUi.Box("charwin-look__sliders");
            for (int i = 0; i < CharacterBodySliders.All.Length; i++)
            {
                grid.Add(BuildSlider(CharacterBodySliders.All[i], i % 2 == 1, draft));
            }

            section.Add(grid);
            section.Add(LobbyUi.Text("Skin tone", "charwin-tab__label"));
            section.Add(Swatches("charwin-skin", Palette(_catalog != null ? _catalog.SkinColors : null), draft.SkinColorIndex, index =>
            {
                _model.Working.SkinColorIndex = index;
                return CharacterLookChange.Look;
            }));
            section.Add(LobbyUi.Text("Eye colour", "charwin-tab__label"));
            section.Add(Swatches("charwin-eyes", Palette(_catalog != null ? _catalog.EyeColors : null), draft.EyeColorIndex, index =>
            {
                _model.Working.EyeColorIndex = index;
                return CharacterLookChange.Look;
            }));
            return section;
        }

        private VisualElement BuildSlider(CharacterBodySlider def, bool rightColumn, SavedCharacter draft)
        {
            VisualElement cell = LobbyUi.Box("charwin-look__slider-cell");
            cell.EnableInClassList("charwin-look__slider-cell--right", rightColumn);
            VisualElement head = LobbyUi.Box("charwin-look__slider-head");
            head.Add(LobbyUi.Text(def.Label, "charwin-look__slider-label"));
            float value = draft.GetMorph(def.Key, def.DefaultValue);
            Label shown = LobbyUi.Mono(Percent(value), "charwin-look__slider-value");
            head.Add(shown);
            cell.Add(head);

            Slider slider = new(0f, 1f) { name = "body-" + def.Key, value = value, tooltip = def.Label };
            slider.AddToClassList("lobby__slider");
            slider.AddToClassList("charwin-look__slider");
            string key = def.Key;
            slider.RegisterValueChangedCallback(e =>
            {
                _model.Working.SetMorph(key, e.newValue);
                shown.text = Percent(e.newValue);
                Changed?.Invoke(CharacterLookChange.Body);
            });
            cell.Add(slider);
            return cell;
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture);

        // ---- Style ----

        private VisualElement BuildStyle(SavedCharacter draft)
        {
            VisualElement section = LobbyUi.Box("charwin-look__section");
            section.Add(LobbyUi.Text("STYLE", "charwin-tab__eyebrow"));
            Color[] hairColors = Palette(_catalog != null ? _catalog.HairColors : null);

            section.Add(LobbyUi.Text("Hair", "charwin-tab__label"));
            section.Add(StyleTiles("hair", _catalog != null ? _catalog.HairStyles : null, draft.HairStyleIndex, index =>
            {
                _model.Working.HairStyleIndex = index;
            }));
            section.Add(ColourRow("charwin-hair-colour", hairColors, draft.HairColorIndex, index => _model.Working.HairColorIndex = index));

            section.Add(LobbyUi.Text("Facial hair", "charwin-tab__label"));
            section.Add(StyleTiles("beard", _catalog != null ? _catalog.BeardStyles : null, draft.BeardStyleIndex, index =>
            {
                _model.Working.BeardStyleIndex = index;
            }));
            section.Add(ColourRow("charwin-beard-colour", hairColors, draft.BeardColorIndex, index => _model.Working.BeardColorIndex = index));

            section.Add(LobbyUi.Text("Eyebrows", "charwin-tab__label"));
            section.Add(EyebrowTiles(draft));
            section.Add(ColourRow("charwin-eyebrow-colour", hairColors, draft.EyebrowColorIndex, index => _model.Working.EyebrowColorIndex = index));
            return section;
        }

        /// <summary>A catalog palette as an array; empty without a catalog.</summary>
        private static Color[] Palette(IReadOnlyList<Color> colors)
        {
            Color[] palette = new Color[colors?.Count ?? 0];
            for (int i = 0; i < palette.Length; i++) palette[i] = colors[i];
            return palette;
        }

        /// <summary>Hair or facial hair: index 0 is None.</summary>
        private VisualElement StyleTiles(string kind, IReadOnlyList<LobbyNamedPrefab> styles, int selected, Action<int> select)
        {
            VisualElement tiles = LobbyUi.Box("charwin-look__tiles");
            tiles.name = "charwin-" + kind + "-styles";
            int count = styles?.Count ?? 1;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                string label = i == 0 || styles == null ? "None" : DisplayName(NameOf(styles[i]));
                string id = i == 0 || styles == null ? "none" : styles[i].Id;
                tiles.Add(Tile(kind + "-" + i, label, Thumbnail(kind, id), i == 0, i == selected, () => select(index)));
            }

            return tiles;
        }

        private VisualElement EyebrowTiles(SavedCharacter draft)
        {
            VisualElement tiles = LobbyUi.Box("charwin-look__tiles");
            tiles.name = "charwin-eyebrow-styles";
            string current = CharacterAppearance.NormalizeEyebrowStyle(draft.Eyebrows);
            tiles.Add(Tile("eyebrows-none", "None", Thumbnail("eyebrows", "none"), true, current == "none", () => _model.Working.Eyebrows = "none"));
            if (_catalog == null || _catalog.EyebrowStyles == null) return tiles;
            foreach (LobbyNamedPrefab style in _catalog.EyebrowStyles)
            {
                string id = CharacterAppearance.NormalizeEyebrowStyle(style.Id);
                tiles.Add(Tile("eyebrows-" + id, DisplayName(NameOf(style)), Thumbnail("eyebrows", id), false, current == id, () => _model.Working.Eyebrows = id));
            }

            return tiles;
        }

        /// <summary>The baked head render for a style, or null (the tile then shows a generic head).</summary>
        private Sprite Thumbnail(string kind, string id) =>
            _catalog != null && _catalog.TryGetStyleThumbnail(kind, id, out Sprite sprite) ? sprite : null;

        private Button Tile(string name, string label, Sprite thumbnail, bool none, bool selected, Action select)
        {
            Button tile = new() { name = name, tooltip = label };
            tile.AddToClassList("charwin-look__tile");
            tile.EnableInClassList("charwin-look__tile--none", none && thumbnail == null);
            tile.EnableInClassList("charwin-look__tile--selected", selected);
            if (thumbnail != null)
            {
                VisualElement image = LobbyUi.Box("charwin-look__tile-thumb");
                image.pickingMode = PickingMode.Ignore;
                image.style.backgroundImage = new StyleBackground(thumbnail);
                tile.Add(image);
            }
            else
            {
                VisualElement icon = LobbyUi.Icon("user");
                icon.AddToClassList("charwin-look__tile-icon");
                tile.Add(icon);
            }

            tile.Add(LobbyUi.Text(label, "charwin-look__tile-label"));
            tile.clicked += () =>
            {
                select();
                MarkSelected(tile, "charwin-look__tile--selected");
                Changed?.Invoke(CharacterLookChange.Look);
            };
            return tile;
        }

        private VisualElement ColourRow(string name, Color[] colors, int selected, Action<int> select)
        {
            VisualElement row = LobbyUi.Box("charwin-look__colour-row");
            row.Add(LobbyUi.Text("Colour", "charwin-look__colour-label"));
            row.Add(Swatches(name, colors, selected, index =>
            {
                select(index);
                return CharacterLookChange.Look;
            }));
            return row;
        }

        private VisualElement Swatches(string name, Color[] colors, int selected, Func<int, CharacterLookChange> select)
        {
            VisualElement row = LobbyUi.Box("charwin-look__swatches");
            row.name = name;
            for (int i = 0; i < colors.Length; i++)
            {
                int index = i;
                Button swatch = new() { tooltip = "#" + ColorUtility.ToHtmlStringRGB(colors[i]) };
                swatch.AddToClassList("charwin-look__swatch");
                swatch.EnableInClassList("charwin-look__swatch--selected", i == Mathf.Clamp(selected, 0, colors.Length - 1));
                swatch.style.backgroundColor = colors[i];
                swatch.clicked += () =>
                {
                    CharacterLookChange change = select(index);
                    MarkSelected(swatch, "charwin-look__swatch--selected");
                    Changed?.Invoke(change);
                };
                row.Add(swatch);
            }

            return row;
        }

        private static void MarkSelected(VisualElement chosen, string selectedClass)
        {
            foreach (VisualElement sibling in chosen.parent.Children())
            {
                sibling.EnableInClassList(selectedClass, sibling == chosen);
            }
        }

        private static Button ActionButton(string name, string icon, string label, Action onClick)
        {
            Button button = new(onClick) { name = name };
            button.AddToClassList("charwin__action");
            button.Add(LobbyUi.Icon(icon));
            button.Add(LobbyUi.Titling(label, "charwin__action-label"));
            return button;
        }

        /// <summary>The prefab's name keeps its capitals ("HumanHairShortMessy"); the catalog id is lower case.</summary>
        private static string NameOf(LobbyNamedPrefab style) => style.Prefab != null ? style.Prefab.name : style.Id;

        /// <summary>"HumanHairSidePart" → "Side Part", "HumanHairAfro2" → "Afro 2".</summary>
        public static string DisplayName(string id)
        {
            string name = id ?? string.Empty;
            foreach (string prefix in new string[] { "HumanHair", "HumanBeard", "HumanFacialHair", "HumanFaceHair", "HumanEyebrows" })
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            name = WordBoundary.Replace(name, "$1 $2").Replace('_', ' ').Replace('-', ' ').Trim();
            return name.Length == 0 ? "Style" : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}
