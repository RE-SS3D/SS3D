using System;
using System.Collections.Generic;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
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
    /// The character window's appearance (lobby.md §6, lobby effort Phase 5), as five panes of the UI bible's centre
    /// column: Profile (the name with Randomize name and its error line, the window's job outfit preview, Randomize
    /// appearance), Body (six sliders), Skin &amp; eyes (skin and eye swatches, eyebrow styles and colour), Hair and Facial
    /// hair (a colour row and a grid of styles). Each label sits over its control. It edits
    /// <see cref="CharacterWindowModel.Working"/>.
    /// </summary>
    public sealed class CharacterAppearanceTab
    {
        private static readonly System.Text.RegularExpressions.Regex WordBoundary = new("([a-z])([A-Z0-9])");

        /// <summary>A style grid's tiles pop in this far apart the first time it shows, after the pane has started to.</summary>
        private const long TileStepMs = 25;
        private const long TileFirstMs = 70;

        private readonly CharacterWindowModel _model;
        private readonly LobbyAssetCatalog _catalog;
        private readonly InputTextEntryScope _textEntry;
        private readonly Func<string> _randomName;
        private readonly Action<SavedCharacter> _randomizeLook;
        private readonly VisualElement _outfit;
        private readonly Dictionary<string, Label> _styleNames = new();
        private TextField _name;
        private VisualElement _error;
        private Label _errorText;

        public event Action<CharacterLookChange> Changed;

        /// <param name="randomName">A new random name (Randomize name).</param>
        /// <param name="randomizeLook">Re-rolls the look of the draft it's given (Randomize appearance).</param>
        /// <param name="outfit">The window's job outfit preview control, shown on the Profile pane.</param>
        public CharacterAppearanceTab(CharacterWindowModel model, LobbyAssetCatalog catalog, InputTextEntryScope textEntry,
            Func<string> randomName, Action<SavedCharacter> randomizeLook, VisualElement outfit)
        {
            _model = model;
            _catalog = catalog;
            _textEntry = textEntry;
            _randomName = randomName;
            _randomizeLook = randomizeLook;
            _outfit = outfit;
            Profile = Pane("profile");
            Body = Pane("body");
            Skin = Pane("skin");
            Hair = Pane("hair");
            Beard = Pane("beard");
        }

        public VisualElement Profile { get; }

        public VisualElement Body { get; }

        public VisualElement Skin { get; }

        public VisualElement Hair { get; }

        public VisualElement Beard { get; }

        /// <summary>The name field; the window focuses it when it opens.</summary>
        public TextField NameField => _name;

        /// <summary>Rebuilds every control from the working copy (a load, a discard, a randomize).</summary>
        public void Rebuild()
        {
            // A focused field removed by the rebuild gets no FocusOut.
            _textEntry.Exit();
            foreach (VisualElement pane in new VisualElement[] { Profile, Body, Skin, Hair, Beard }) pane.Clear();
            _styleNames.Clear();
            SavedCharacter draft = _model.Working;
            if (draft == null) return;

            BuildProfile(draft);
            BuildBody(draft);
            BuildSkin(draft);
            BuildHair(draft);
            BuildBeard(draft);
            ShowNameError();
        }

        /// <summary>A plain column, not a scroll area: the style grids are wider than it, as in the bible, and would be clipped.</summary>
        private static VisualElement Pane(string key)
        {
            VisualElement pane = LobbyUi.Box("charwin__pane");
            pane.name = "charwin-pane-" + key;
            return pane;
        }

        /// <summary>A label centred over its control, as the reference, between two hairlines that mark the section.</summary>
        private static VisualElement Field(string label, string extraClass = null)
        {
            VisualElement field = LobbyUi.Box("charwin__field");
            if (extraClass != null) field.AddToClassList(extraClass);
            VisualElement head = LobbyUi.Box("charwin__label-row");
            head.pickingMode = PickingMode.Ignore;
            head.Add(LobbyUi.Box("charwin__label-line"));
            head.Add(LobbyUi.Text(label, "charwin__label"));
            head.Add(LobbyUi.Box("charwin__label-line"));
            field.Add(head);
            return field;
        }

        // ---- Profile ----

        private void BuildProfile(SavedCharacter draft)
        {
            VisualElement name = Field("name", "charwin__field--tight");
            _name = new TextField { name = "charwin-name", value = draft.Name, maxLength = 40 };
            _name.AddToClassList("charwin__text-field");
            _name.textEdition.placeholder = "First Last";
            _name.RegisterValueChangedCallback(e =>
            {
                _model.Working.Name = e.newValue ?? string.Empty;
                ShowNameError();
                Changed?.Invoke(CharacterLookChange.Name);
            });
            LobbyUi.HoldTextEntry(_name, _textEntry);
            LobbyUi.MenuInput(_name);
            name.Add(_name);
            Profile.Add(name);

            _error = LobbyUi.Box("charwin-look__error");
            _error.name = "charwin-name-error";
            _error.Add(LobbyUi.Icon("warning"));
            _errorText = LobbyUi.Text("", "charwin-look__error-text");
            _error.Add(_errorText);
            Profile.Add(_error);

            Button randomName = SmallButton("charwin-randomize-name", "randomize name", () => _name.value = _randomName());
            randomName.AddToClassList("charwin__small--under");
            Profile.Add(randomName);

            if (_outfit != null)
            {
                VisualElement outfit = Field("job outfit preview");
                outfit.Add(_outfit);
                Profile.Add(outfit);
            }

            Profile.Add(LobbyUi.Box("charwin__divider"));
            VisualElement rest = Field("everything else");
            rest.Add(SmallButton("charwin-randomize-look", "randomize appearance", () =>
            {
                _randomizeLook(_model.Working);
                Rebuild();
                Changed?.Invoke(CharacterLookChange.Body);
                Changed?.Invoke(CharacterLookChange.Look);
            }));
            Profile.Add(rest);
            Profile.Add(LobbyUi.Text("Everything here is visible to other players.", "charwin__note"));
        }

        /// <summary>Repaints the name's error line (the only state not rebuilt per edit).</summary>
        public void Refresh() => ShowNameError();

        private void ShowNameError()
        {
            if (_error == null) return;
            string text = CharacterNameRules.Describe(_model.NameError);
            bool appearing = text.Length > 0 && _error.ClassListContains("charwin-look__error--hidden");
            _errorText.text = text;
            _error.EnableInClassList("charwin-look__error--hidden", text.Length == 0);
            _name.EnableInClassList("charwin-look__name--error", text.Length > 0);
            if (appearing) CharacterWindowView.Enter(_error);
        }

        // ---- Body ----

        private void BuildBody(SavedCharacter draft)
        {
            foreach (CharacterBodySlider def in CharacterBodySliders.All)
            {
                VisualElement field = Field(def.Label.ToLowerInvariant());
                Slider slider = new(0f, 1f)
                {
                    name = "body-" + def.Key,
                    value = draft.GetMorph(def.Key, def.DefaultValue),
                    tooltip = def.Label,
                    fill = true,
                };
                slider.AddToClassList("charwin__slider");
                string key = def.Key;
                slider.RegisterValueChangedCallback(e =>
                {
                    _model.Working.SetMorph(key, e.newValue);
                    Changed?.Invoke(CharacterLookChange.Body);
                });
                field.Add(slider);
                Body.Add(field);
            }
        }

        // ---- Skin & eyes ----

        private void BuildSkin(SavedCharacter draft)
        {
            VisualElement skin = Field("skin tone");
            skin.Add(Swatches("charwin-skin", Palette(_catalog != null ? _catalog.SkinColors : null), draft.SkinColorIndex,
                index => _model.Working.SkinColorIndex = index));
            Skin.Add(skin);

            VisualElement eyes = Field("eye colour");
            eyes.Add(Swatches("charwin-eyes", Palette(_catalog != null ? _catalog.EyeColors : null), draft.EyeColorIndex,
                index => _model.Working.EyeColorIndex = index));
            Skin.Add(eyes);

            VisualElement brows = Field("eyebrows", "charwin__field--styles");
            Label browName = StyleName("eyebrows");
            brows.Add(EyebrowTiles(draft));
            brows.Add(browName);
            Skin.Add(brows);

            VisualElement browColour = Field("eyebrow colour", "charwin__field--tight");
            browColour.Add(Swatches("charwin-eyebrow-colour", HairPalette, draft.EyebrowColorIndex,
                index => _model.Working.EyebrowColorIndex = index));
            Skin.Add(browColour);
        }

        // ---- Hair and facial hair ----

        private void BuildHair(SavedCharacter draft)
        {
            VisualElement colour = Field("colour");
            colour.Add(Swatches("charwin-hair-colour", HairPalette, draft.HairColorIndex, index => _model.Working.HairColorIndex = index));
            Hair.Add(colour);
            VisualElement style = Field("style", "charwin__field--tight");
            Label name = StyleName("hair");
            style.Add(StyleTiles("hair", _catalog != null ? _catalog.HairStyles : null, draft.HairStyleIndex,
                index => _model.Working.HairStyleIndex = index));
            style.Add(name);
            Hair.Add(style);
        }

        private void BuildBeard(SavedCharacter draft)
        {
            VisualElement colour = Field("colour");
            colour.Add(Swatches("charwin-beard-colour", HairPalette, draft.BeardColorIndex, index => _model.Working.BeardColorIndex = index));
            Beard.Add(colour);
            VisualElement style = Field("style", "charwin__field--tight");
            Label name = StyleName("beard");
            style.Add(StyleTiles("beard", _catalog != null ? _catalog.BeardStyles : null, draft.BeardStyleIndex,
                index => _model.Working.BeardStyleIndex = index));
            style.Add(name);
            Beard.Add(style);
        }

        private Color[] HairPalette => Palette(_catalog != null ? _catalog.HairColors : null);

        /// <summary>A catalog palette as an array; empty without a catalog.</summary>
        private static Color[] Palette(IReadOnlyList<Color> colors)
        {
            Color[] palette = new Color[colors?.Count ?? 0];
            for (int i = 0; i < palette.Length; i++) palette[i] = colors[i];
            return palette;
        }

        /// <summary>Under a grid: the picked style's name. Made before the grid, which names its picked tile.</summary>
        private Label StyleName(string kind)
        {
            Label label = LobbyUi.Text("", "charwin__style-name");
            label.name = "charwin-" + kind + "-style-name";
            _styleNames[kind] = label;
            return label;
        }

        private void ShowStyleName(string kind, string name)
        {
            if (_styleNames.TryGetValue(kind, out Label label)) label.text = name.ToLowerInvariant();
        }

        /// <summary>The reference's style tiles, three across, in a scroll area; the tiles are its children.</summary>
        private static ScrollView Grid(string kind, bool shortGrid)
        {
            ScrollView grid = new(ScrollViewMode.Vertical) { name = "charwin-" + kind + "-styles" };
            grid.AddToClassList("charwin__styles");
            grid.EnableInClassList("charwin__styles--short", shortGrid);
            grid.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            grid.contentContainer.AddToClassList("charwin__styles-content");
            StaggerOnShow(grid);
            return grid;
        }

        /// <summary>
        /// The tiles are built in UiMotion's hidden look (<see cref="Tile"/>): the first time the grid is laid out (its
        /// tab opened, or rebuilt while open), they pop in one after another.
        /// </summary>
        private static void StaggerOnShow(ScrollView grid)
        {
            void OnShown(GeometryChangedEvent evt)
            {
                if (evt.newRect.height <= 0f) return;
                grid.UnregisterCallback<GeometryChangedEvent>(OnShown);
                CharacterWindowView.Stagger(grid, TileStepMs, TileFirstMs, CharacterWindowView.EnterScaled);
            }

            grid.RegisterCallback<GeometryChangedEvent>(OnShown);
        }

        /// <summary>Hair or facial hair: index 0 is None.</summary>
        private ScrollView StyleTiles(string kind, IReadOnlyList<LobbyNamedPrefab> styles, int selected, Action<int> select)
        {
            ScrollView tiles = Grid(kind, false);
            int count = styles?.Count ?? 1;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                string label = i == 0 || styles == null ? "None" : DisplayName(NameOf(styles[i]));
                string id = i == 0 || styles == null ? "none" : styles[i].Id;
                if (i == selected) ShowStyleName(kind, label);
                Button tile = Tile(kind, kind + "-" + i, label, Thumbnail(kind, id), i == selected, () => select(index));
                tiles.Add(tile);
                if (i == selected) ScrollToOnShow(tiles, tile);
            }

            return tiles;
        }

        private ScrollView EyebrowTiles(SavedCharacter draft)
        {
            ScrollView tiles = Grid("eyebrow", true);
            string current = CharacterAppearance.NormalizeEyebrowStyle(draft.Eyebrows);
            if (current == "none") ShowStyleName("eyebrows", "None");
            tiles.Add(Tile("eyebrows", "eyebrows-none", "None", Thumbnail("eyebrows", "none"), current == "none",
                () => _model.Working.Eyebrows = "none"));
            if (_catalog == null || _catalog.EyebrowStyles == null) return tiles;
            foreach (LobbyNamedPrefab style in _catalog.EyebrowStyles)
            {
                string id = CharacterAppearance.NormalizeEyebrowStyle(style.Id);
                string label = DisplayName(NameOf(style));
                if (current == id) ShowStyleName("eyebrows", label);
                tiles.Add(Tile("eyebrows", "eyebrows-" + id, label, Thumbnail("eyebrows", id), current == id,
                    () => _model.Working.Eyebrows = id));
            }

            return tiles;
        }

        /// <summary>The grid is built hidden (its tab closed): once it is first laid out, bring the picked style into view.</summary>
        private static void ScrollToOnShow(ScrollView grid, VisualElement picked)
        {
            void OnShown(GeometryChangedEvent evt)
            {
                if (evt.newRect.height <= 0f) return;
                grid.UnregisterCallback<GeometryChangedEvent>(OnShown);
                grid.schedule.Execute(() => grid.ScrollTo(picked));
            }

            grid.RegisterCallback<GeometryChangedEvent>(OnShown);
        }

        /// <summary>The baked head render for a style, or null (the tile then shows a generic head).</summary>
        private Sprite Thumbnail(string kind, string id) =>
            _catalog != null && _catalog.TryGetStyleThumbnail(kind, id, out Sprite sprite) ? sprite : null;

        private Button Tile(string kind, string name, string label, Sprite thumbnail, bool selected, Action select)
        {
            Button tile = new() { name = name, tooltip = label };
            tile.AddToClassList("charwin-look__tile");
            tile.EnableInClassList("charwin-look__tile--selected", selected);
            // Hidden until its grid first shows (StaggerOnShow), so it never flashes in before it pops in.
            tile.AddToClassList(CharacterWindowView.MotionClass);
            tile.AddToClassList(CharacterWindowView.EnterScaled);
            tile.AddToClassList(UiMotion.EnterClass);
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

            // The picked tile's corner badge; CharacterWindow.uss pops it in with charwin-look__tile--selected.
            VisualElement check = LobbyUi.Icon("check");
            check.AddToClassList("charwin-look__tile-check");
            tile.Add(check);

            tile.clicked += () =>
            {
                select();
                MarkSelected(tile, "charwin-look__tile--selected");
                ShowStyleName(kind, label);
                Changed?.Invoke(CharacterLookChange.Look);
            };
            return tile;
        }

        private VisualElement Swatches(string name, Color[] colors, int selected, Action<int> select)
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
                    select(index);
                    MarkSelected(swatch, "charwin-look__swatch--selected");
                    Changed?.Invoke(CharacterLookChange.Look);
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

        private static Button SmallButton(string name, string label, Action onClick)
        {
            Button button = new(onClick) { name = name, text = label };
            button.AddToClassList("charwin__small");
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
