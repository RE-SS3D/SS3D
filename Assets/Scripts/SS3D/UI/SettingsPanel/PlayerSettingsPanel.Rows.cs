using System;
using System.Linq;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using static SS3D.UI.SettingsPanel.SettingsUi;

namespace SS3D.UI.SettingsPanel
{
    /// <summary>The Display and Audio tabs: titled groups of rows, each a label, an optional note and a control.</summary>
    public sealed partial class PlayerSettingsPanel
    {
        private static readonly string[] OffOn = { "Off", "On" };
        private static readonly string[] WindowModes = { "Fullscreen", "Borderless", "Windowed" };
        private static readonly string[] Qualities = { "Low", "Medium", "High", "Ultra" };
        private static readonly string[] Upscalers = { "STP", "FSR 1", "Bilinear" };
        private static readonly string[] AntiAliasings = { "Off", "FXAA", "SMAA", "TAA" };

        private static readonly PlayerDisplaySettings DisplayDefaults = new();
        private static readonly PlayerAudioSettings AudioDefaults = new();

        private PlayerDisplaySettings Display => _settings.Settings.Display;
        private PlayerAudioSettings Audio => _settings.Settings.Audio;

        private enum NoteTone { Secondary = 0, Warning = 1, Steel = 2 }

        /// <summary>A row's note and lock, worked out on every refresh.</summary>
        private readonly struct RowState
        {
            public readonly string Note;
            public readonly NoteTone Tone;
            public readonly bool Locked;
            public readonly int Effective;

            public RowState(string note, NoteTone tone = NoteTone.Secondary, bool locked = false, int effective = -1)
            {
                Note = note;
                Tone = tone;
                Locked = locked;
                Effective = effective;
            }
        }

        private sealed class SettingRow
        {
            public VisualElement Root;
            public VisualElement Dot;
            public VisualElement Note;
            public Label NoteText;
            public VisualElement NoteLock;
            public VisualElement Control;

            public void SetChanged(bool changed) => Dot.style.display = changed ? DisplayStyle.Flex : DisplayStyle.None;

            public void SetNote(string text, NoteTone tone, bool locked)
            {
                Note.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                NoteText.text = text ?? "";
                Note.EnableInClassList("settings-panel__note--warning", tone == NoteTone.Warning);
                Note.EnableInClassList("settings-panel__note--steel", tone == NoteTone.Steel);
                NoteLock.style.display = locked ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void BuildDisplay()
        {
            VisualElement screen = Group("Screen");
            ResolutionRow(screen);
            Segmented(screen, "windowMode", "Window mode", WindowModes, () => (int)Display.WindowMode,
                (int)DisplayDefaults.WindowMode, i => _settings.SetWindowMode((PlayerWindowMode)i));
            Segmented(screen, "vsync", "VSync", OffOn, () => Display.VSync ? 1 : 0, DisplayDefaults.VSync ? 1 : 0,
                i => _settings.SetVSync(i == 1));
            int[] caps = PlayerDisplaySettings.FrameCaps;
            Segmented(screen, "frameCap", "Frame cap", caps.Select(c => c == 0 ? "Off" : c.ToString()).ToArray(),
                () => Array.IndexOf(caps, Display.FrameCap), Array.IndexOf(caps, DisplayDefaults.FrameCap),
                i => _settings.SetFrameCap(caps[i]),
                () => Display.VSync ? new RowState("Not used while VSync is on.", locked: true) : default);

            VisualElement rendering = Group("Rendering");
            Segmented(rendering, "quality", "Quality preset", Qualities, () => (int)Display.Quality,
                (int)DisplayDefaults.Quality, i => _settings.SetQuality((PlayerQualityTier)i),
                () => new RowState("Shadow distance, shadow resolution and similar."));
            float[] scales = PlayerDisplaySettings.RenderScales;
            Segmented(rendering, "renderScale", "Render scale", scales.Select(s => Mathf.RoundToInt(s * 100f) + " %").ToArray(),
                () => Array.IndexOf(scales, Display.RenderScale), Array.IndexOf(scales, DisplayDefaults.RenderScale),
                i => _settings.SetRenderScale(scales[i]));
            Segmented(rendering, "upscaler", "Upscaler", Upscalers, () => (int)Display.Upscaler,
                (int)DisplayDefaults.Upscaler, i => _settings.SetUpscaler((PlayerUpscaler)i), UpscalerState);
            Segmented(rendering, "antiAliasing", "Anti-aliasing", AntiAliasings, () => (int)Display.AntiAliasing,
                (int)DisplayDefaults.AntiAliasing, i => _settings.SetAntiAliasing((PlayerAntiAliasing)i), AntiAliasingState);

            VisualElement ui = Group("Interface");
            PercentSlider(ui, "uiScale", "UI scale", Mathf.RoundToInt(PlayerDisplaySettings.MinUiScale * 100f),
                Mathf.RoundToInt(PlayerDisplaySettings.MaxUiScale * 100f), () => Mathf.RoundToInt(Display.UiScale * 100f),
                Mathf.RoundToInt(DisplayDefaults.UiScale * 100f), v => _settings.SetUiScale(v / 100f), "Applies as you drag.");
            Segmented(ui, "showFps", "Show FPS counter", OffOn, () => Display.ShowFps ? 1 : 0, DisplayDefaults.ShowFps ? 1 : 0,
                i => _settings.SetShowFps(i == 1));
            Segmented(ui, "showLogIndicator", "Show log indicator", OffOn, () => Display.ShowLogIndicator ? 1 : 0,
                DisplayDefaults.ShowLogIndicator ? 1 : 0, i => _settings.SetShowLogIndicator(i == 1),
                () => new RowState("On-screen error and warning count, for bug reports."));
        }

        private RowState UpscalerState()
        {
            if (Display.Upscaler == PlayerUpscaler.Stp && !_settings.StpSupported)
                return new RowState("This GPU can’t run STP, so FSR 1 is used instead.", NoteTone.Warning);
            return Display.RenderScale >= 1f ? new RowState("Only upscales below 100 % render scale.") : default;
        }

        private RowState AntiAliasingState()
        {
            RenderPlan plan = PlayerDisplayPlan.Resolve(Display, _settings.StpSupported);
            if (!plan.AntiAliasingLocked) return default;
            string back = Display.AntiAliasing == PlayerAntiAliasing.Taa ? "." : "; " + AntiAliasings[(int)Display.AntiAliasing] + " comes back.";
            return new RowState("Locked to TAA: STP needs it. Pick FSR 1 or Bilinear to choose" + back, NoteTone.Steel, true,
                (int)plan.AntiAliasing);
        }

        private void BuildAudio()
        {
            VisualElement volume = Group("Volume");
            foreach ((PlayerVolume channel, string key, string label) in new (PlayerVolume, string, string)[]
            {
                (PlayerVolume.Master, "masterVolume", "Master"),
                (PlayerVolume.Music, "musicVolume", "Music"),
                (PlayerVolume.Sfx, "sfxVolume", "SFX"),
                (PlayerVolume.Ambience, "ambienceVolume", "Ambience"),
                (PlayerVolume.Personal, "personalVolume", "Personal"),
            })
            {
                PercentSlider(volume, key, label, 0, 100, () => Mathf.RoundToInt(Audio.Get(channel) * 100f),
                    Mathf.RoundToInt(AudioDefaults.Get(channel) * 100f), v => _settings.SetVolume(channel, v / 100f),
                    dimmed: () => Audio.MuteAll);
            }

            VisualElement muting = Group("Muting");
            Segmented(muting, "muteAll", "Mute all", OffOn, () => Audio.MuteAll ? 1 : 0, AudioDefaults.MuteAll ? 1 : 0,
                i => _settings.SetMuteAll(i == 1), () => new RowState("Silences output. Levels stay as set."));
            Segmented(muting, "muteUnfocused", "Mute when window unfocused", OffOn, () => Audio.MuteWhenUnfocused ? 1 : 0,
                AudioDefaults.MuteWhenUnfocused ? 1 : 0, i => _settings.SetMuteWhenUnfocused(i == 1));

            VisualElement menu = Group("Main menu");
            Segmented(menu, "menuMusic", "Main menu music", OffOn, () => Audio.MenuMusic ? 1 : 0, AudioDefaults.MenuMusic ? 1 : 0,
                i => _settings.SetMenuMusic(i == 1), () => new RowState("Plays on the main menu only."));
        }

        private VisualElement Group(string title)
        {
            VisualElement group = Element(_body, "settings-panel__group");
            Text(group, title.ToUpperInvariant(), "settings-panel__group-title");
            return group;
        }

        private SettingRow Row(VisualElement group, string key, string label)
        {
            SettingRow row = new() { Root = Element(group, "settings-panel__row") };
            row.Root.name = "settings-row-" + key;
            VisualElement copy = Element(row.Root, "settings-panel__row-copy");
            VisualElement line = Element(copy, "settings-panel__row-line");
            Text(line, label, "settings-panel__row-label");
            row.Dot = Element(line, "settings-panel__changed");
            row.Dot.tooltip = "Changed from default";
            row.Note = Element(copy, "settings-panel__note");
            row.NoteLock = LockIcon();
            row.Note.Add(row.NoteLock);
            row.NoteText = Text(row.Note, "", "settings-panel__note-text");
            row.Control = Element(row.Root, "settings-panel__control");
            return row;
        }

        /// <summary>One button per choice; a locked row shows its effective choice with a padlock and takes no clicks.</summary>
        private void Segmented(VisualElement group, string key, string label, string[] options, Func<int> current, int defaultIndex,
            Action<int> pick, Func<RowState> state = null)
        {
            SettingRow row = Row(group, key, label);
            row.Control.AddToClassList("settings-panel__segments");
            Button[] buttons = new Button[options.Length];
            VisualElement[] locks = new VisualElement[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                Button button = new(() =>
                {
                    if (index != current()) pick(index);
                }) { name = $"settings-{key}-{i}" };
                button.AddToClassList("settings-panel__segment");
                locks[i] = LockIcon();
                button.Add(locks[i]);
                Label text = new(options[i]) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("settings-panel__segment-text");
                button.Add(text);
                buttons[i] = button;
                row.Control.Add(button);
            }

            _refreshers.Add(() =>
            {
                int value = current();
                RowState s = state?.Invoke() ?? default;
                int shown = s.Locked && s.Effective >= 0 ? s.Effective : value;
                for (int i = 0; i < buttons.Length; i++)
                {
                    bool on = i == shown;
                    buttons[i].SetEnabled(!s.Locked);
                    buttons[i].EnableInClassList("settings-panel__segment--on", on && !s.Locked);
                    buttons[i].EnableInClassList("settings-panel__segment--locked-on", on && s.Locked);
                    buttons[i].EnableInClassList("settings-panel__segment--locked-off", !on && s.Locked);
                    locks[i].style.display = on && s.Locked ? DisplayStyle.Flex : DisplayStyle.None;
                }
                row.SetChanged(value != defaultIndex);
                row.SetNote(s.Note, s.Tone, s.Locked);
            });
        }

        /// <summary>A whole-percent slider with its value beside it; applies on every step of a drag.</summary>
        private void PercentSlider(VisualElement group, string key, string label, int min, int max, Func<int> current,
            int defaultValue, Action<int> changed, string note = null, Func<bool> dimmed = null)
        {
            SettingRow row = Row(group, key, label);
            row.Control.AddToClassList("settings-panel__slider-control");
            SliderInt slider = new(min, max) { name = "settings-" + key, value = current() };
            slider.AddToClassList("settings-panel__slider");
            VisualElement fill = new() { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("settings-panel__slider-fill");
            slider.Q(className: "unity-base-slider__tracker")?.Add(fill);
            Label value = Text(row.Control, "", "settings-panel__mono settings-panel__slider-value");
            value.name = "settings-value-" + key;
            row.Control.Insert(0, slider);

            void Show(int v)
            {
                value.text = v + " %";
                fill.style.width = Length.Percent((v - min) * 100f / (max - min));
            }

            slider.RegisterValueChangedCallback(evt =>
            {
                Show(evt.newValue);
                if (evt.newValue != current()) changed(evt.newValue);
            });

            _refreshers.Add(() =>
            {
                int v = current();
                if (slider.value != v) slider.SetValueWithoutNotify(v);
                Show(v);
                value.EnableInClassList("settings-panel__slider-value--dim", dimmed?.Invoke() == true);
                row.SetChanged(v != defaultValue);
                row.SetNote(note, NoteTone.Secondary, false);
            });
        }

        private void ResolutionRow(VisualElement group)
        {
            SettingRow row = Row(group, "resolution", "Resolution");
            DropdownField dropdown = new() { name = "settings-resolution" };
            dropdown.AddToClassList("settings-panel__select");
            DarkInput(dropdown);
            row.Control.Add(dropdown);
            Vector2Int[] choices = Array.Empty<Vector2Int>();
            dropdown.RegisterValueChangedCallback(_ =>
            {
                int index = dropdown.index;
                if (index >= 0 && index < choices.Length && choices[index] != _settings.CurrentResolution)
                    _settings.SetResolution(choices[index]);
            });

            _refreshers.Add(() =>
            {
                Vector2Int current = _settings.CurrentResolution;
                choices = _settings.ResolutionChoices;
                if (Array.IndexOf(choices, current) < 0) choices = choices.Prepend(current).ToArray();
                dropdown.choices = choices.Select(ResolutionChoiceText).ToList();
                dropdown.SetValueWithoutNotify(ResolutionChoiceText(current));
                row.SetChanged(current != _settings.NativeResolution);
                row.SetNote("Your monitor’s supported resolutions.", NoteTone.Secondary, false);
            });
        }

        /// <summary>The runtime theme's popup field outranks a stylesheet class, so the dark field is set inline (as the lobby does).</summary>
        private static void DarkInput(DropdownField dropdown)
        {
            void Apply()
            {
                VisualElement input = dropdown.Q(className: "unity-base-field__input");
                if (input != null)
                {
                    // The theme draws the light field as a background image over any colour.
                    input.style.backgroundImage = new StyleBackground(StyleKeyword.None);
                    input.style.backgroundColor = new Color(0.133f, 0.133f, 0.133f); // #222222
                    Color border = new(0.847f, 0.847f, 0.847f, 0.18f);
                    input.style.borderTopColor = border;
                    input.style.borderBottomColor = border;
                    input.style.borderLeftColor = border;
                    input.style.borderRightColor = border;
                    input.style.color = Color.white;
                    // The input clips its text, and a rounded clipped element renders solid white on Linux.
                    input.style.borderTopLeftRadius = 0f;
                    input.style.borderTopRightRadius = 0f;
                    input.style.borderBottomLeftRadius = 0f;
                    input.style.borderBottomRightRadius = 0f;
                }
                if (dropdown.Q(className: "unity-base-popup-field__text") is { } text) text.style.color = Color.white;
            }

            Apply();
            dropdown.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }

        private string ResolutionChoiceText(Vector2Int size) =>
            ResolutionText(size) + (size == _settings.NativeResolution ? " (native)" : "");

        private static string ResolutionText(Vector2Int size) => $"{size.x} × {size.y}";

        private static string ModeText(PlayerWindowMode mode) => WindowModes[(int)mode];
    }
}
