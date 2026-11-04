using System;
using UnityEngine;

namespace SS3D.Systems.Settings
{
    /// <summary>
    /// Client-local machine options (2026-10_player-settings). Plain data: <see cref="PlayerSettingsSubSystem"/>
    /// applies it and <see cref="PlayerSettingsStore"/> persists it. Field initializers are the first-launch
    /// defaults (Decision 14), and also fill keys a saved file lacks.
    /// </summary>
    [Serializable]
    public sealed class PlayerSettings
    {
        public const int CurrentVersion = 1;

        /// <summary>File format version (Decision 2). Bump with a migration when a field changes meaning.</summary>
        public int Version = CurrentVersion;
        public PlayerAudioSettings Audio = new();
        public PlayerDisplaySettings Display = new();
        public PlayerControlsSettings Controls = new();

        /// <summary>Pulls hand-edited or stale values back into range after a load.</summary>
        public void Sanitize()
        {
            Audio ??= new PlayerAudioSettings();
            Audio.Sanitize();
            Display ??= new PlayerDisplaySettings();
            Display.Sanitize();
            Controls ??= new PlayerControlsSettings();
            Controls.Sanitize();
        }
    }

    /// <summary>The five volume sliders: Master plus the audio.md §7 categories.</summary>
    public enum PlayerVolume
    {
        Master,
        Music,
        Sfx,
        Ambience,
        Personal,
    }

    /// <summary>Linear 0..1 volumes per audio.md §7 category, plus the two mutes (Decision 4).</summary>
    [Serializable]
    public sealed class PlayerAudioSettings
    {
        // Placeholder levels; balancing them is out of scope (audio.md §10).
        public float Master = DefaultVolume;
        public float Music = DefaultVolume;
        public float Sfx = DefaultVolume;
        public float Ambience = DefaultVolume;
        public float Personal = DefaultVolume;
        public bool MuteAll;
        public bool MuteWhenUnfocused = true;

        public float Get(PlayerVolume volume) => volume switch
        {
            PlayerVolume.Master => Master,
            PlayerVolume.Music => Music,
            PlayerVolume.Sfx => Sfx,
            PlayerVolume.Ambience => Ambience,
            _ => Personal,
        };

        public void Set(PlayerVolume volume, float value)
        {
            value = Volume(value);
            switch (volume)
            {
                case PlayerVolume.Master: Master = value; break;
                case PlayerVolume.Music: Music = value; break;
                case PlayerVolume.Sfx: Sfx = value; break;
                case PlayerVolume.Ambience: Ambience = value; break;
                default: Personal = value; break;
            }
        }

        internal void Sanitize()
        {
            Master = Volume(Master);
            Music = Volume(Music);
            Sfx = Volume(Sfx);
            Ambience = Volume(Ambience);
            Personal = Volume(Personal);
        }

        private static float Volume(float value) => float.IsNaN(value) ? DefaultVolume : Mathf.Clamp01(value);

        private const float DefaultVolume = 0.8f;
    }

    public enum PlayerWindowMode { Fullscreen, Borderless, Windowed }

    public enum PlayerQualityTier { Low, Medium, High, Ultra }

    public enum PlayerUpscaler { Stp, Fsr1, Bilinear }

    public enum PlayerAntiAliasing { Off, Fxaa, Smaa, Taa }

    /// <summary>Display tab values (Decisions 5–8). 0×0 resolution means the display's native size.</summary>
    [Serializable]
    public sealed class PlayerDisplaySettings
    {
        /// <summary>Frame cap choices; 0 is Off.</summary>
        public static readonly int[] FrameCaps = { 0, 30, 60, 120, 144, 240 };

        /// <summary>Render scale choices, 100 / 77 / 67 / 50 % (Decision 7).</summary>
        public static readonly float[] RenderScales = { 1f, 0.77f, 0.67f, 0.5f };

        public const float MinUiScale = 0.75f;
        public const float MaxUiScale = 1.5f;
        public const float UiScaleStep = 0.01f;

        public int ResolutionWidth;
        public int ResolutionHeight;
        public PlayerWindowMode WindowMode = PlayerWindowMode.Borderless;
        public bool VSync = true;
        public int FrameCap;
        public PlayerQualityTier Quality = PlayerQualityTier.High;
        public float RenderScale = 1f;
        public PlayerUpscaler Upscaler = PlayerUpscaler.Stp;
        public PlayerAntiAliasing AntiAliasing = PlayerAntiAliasing.Smaa;
        public float UiScale = 1f;
        /// <summary>Small frame-rate readout in the screen corner; off by default.</summary>
        public bool ShowFps;
        /// <summary>The console's info/warning/error counter on the screen edge, for players without a debugger; off by default.</summary>
        public bool ShowLogIndicator;

        public static float SnapUiScale(float value)
        {
            if (float.IsNaN(value)) return 1f;
            float snapped = Mathf.Round(Mathf.Clamp(value, MinUiScale, MaxUiScale) / UiScaleStep) * UiScaleStep;
            return (float)Math.Round(snapped, 2);
        }

        internal void Sanitize()
        {
            if (ResolutionWidth <= 0 || ResolutionHeight <= 0) ResolutionWidth = ResolutionHeight = 0;
            WindowMode = Defined(WindowMode, PlayerWindowMode.Borderless);
            Quality = Defined(Quality, PlayerQualityTier.High);
            Upscaler = Defined(Upscaler, PlayerUpscaler.Stp);
            AntiAliasing = Defined(AntiAliasing, PlayerAntiAliasing.Smaa);
            FrameCap = Nearest(FrameCaps, FrameCap);
            RenderScale = float.IsNaN(RenderScale) ? 1f : Nearest(RenderScales, RenderScale);
            UiScale = SnapUiScale(UiScale);
        }

        private static T Defined<T>(T value, T fallback) where T : struct, Enum =>
            Enum.IsDefined(typeof(T), value) ? value : fallback;

        private static int Nearest(int[] choices, int value)
        {
            int best = choices[0];
            foreach (int choice in choices)
                if (Math.Abs(choice - value) < Math.Abs(best - value)) best = choice;
            return best;
        }

        private static float Nearest(float[] choices, float value)
        {
            float best = choices[0];
            foreach (float choice in choices)
                if (Math.Abs(choice - value) < Math.Abs(best - value)) best = choice;
            return best;
        }
    }

    /// <summary>
    /// Controls tab values (Decisions 2, 9–11). Overrides are <c>SaveBindingOverridesAsJson</c> output: one string for
    /// the generated Controls asset, one for InputSubSystem's code-defined System map. Empty means defaults.
    /// </summary>
    [Serializable]
    public sealed class PlayerControlsSettings
    {
        public string BindingOverrides = string.Empty;
        public string SystemBindingOverrides = string.Empty;

        internal void Sanitize()
        {
            BindingOverrides ??= string.Empty;
            SystemBindingOverrides ??= string.Empty;
        }
    }
}
