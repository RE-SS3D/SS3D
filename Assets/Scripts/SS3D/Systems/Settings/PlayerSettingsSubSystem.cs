using System;
using System.Collections.Generic;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Core;
using SS3D.Systems.Audio;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace SS3D.Systems.Settings
{
    /// <summary>
    /// Client-local owner of <see cref="PlayerSettings"/> (2026-10_player-settings). Loads at boot, applies before
    /// the intro and lobby, and saves shortly after the last change. Views raise "set X" intents here and rebuild
    /// from <see cref="Settings"/> on <see cref="Changed"/>. Inert on a headless or dedicated server.
    /// </summary>
    public sealed class PlayerSettingsSubSystem : SubSystem
    {
        /// <summary>A slider drag fires many changes; write the file once it settles.</summary>
        private const float SaveDebounceSeconds = 0.5f;

        private PlayerSettingsStore _store;
        private bool _active;
        private bool _focused = true;
        private float _saveDueTime = -1f;
        private readonly PlayerDisplayApplier _display = new();
        private readonly DisplayChangeConfirmation<WindowState> _windowConfirmation = new();

        /// <summary>Resolution and window mode as last confirmed; what a revert restores.</summary>
        private readonly struct WindowState
        {
            public readonly int Width;
            public readonly int Height;
            public readonly PlayerWindowMode Mode;

            public WindowState(PlayerDisplaySettings display)
            {
                Width = display.ResolutionWidth;
                Height = display.ResolutionHeight;
                Mode = display.WindowMode;
            }

            public void WriteTo(PlayerDisplaySettings display)
            {
                display.ResolutionWidth = Width;
                display.ResolutionHeight = Height;
                display.WindowMode = Mode;
            }
        }

        /// <summary>Current values. Read-only for callers: change them through the Set methods.</summary>
        public PlayerSettings Settings { get; private set; } = new();

        public event Action Changed;

        public static bool IsHeadless =>
#if UNITY_SERVER
            true;
#else
            UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif

        protected override void OnAwake()
        {
            base.OnAwake();
            if (IsHeadless) return;

            _active = true;
            _store = new PlayerSettingsStore(PlayerSettingsStore.DefaultPath);
            Settings = _store.Load();
            _focused = UnityEngine.Application.isFocused;
            UnityEngine.Application.focusChanged += HandleFocusChanged;
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            _display.Enable();
            _display.ApplyAll(Settings.Display);
        }

        protected override void OnStart()
        {
            base.OnStart();
            // AudioMixer.SetFloat is ignored when called from Awake, so the first apply waits for Start.
            if (!_active) return;
            ApplyAudio();
            // InputSubSystem is created after this one in SystemsBootstrap; by Start it exists.
            ApplyControls();
        }

        protected override void OnDestroyed()
        {
            UnityEngine.Application.focusChanged -= HandleFocusChanged;
            _capture?.Dispose();
            _capture = null;
            if (_active && _windowConfirmation.IsPending) RevertDisplayChange();
            if (_saveDueTime >= 0f) SaveNow();
            if (_active) _display.Disable();
            base.OnDestroyed();
        }

        public void SetVolume(PlayerVolume volume, float linear)
        {
            Settings.Audio.Set(volume, linear);
            CommitAudio();
        }

        public void SetMuteAll(bool muted)
        {
            Settings.Audio.MuteAll = muted;
            CommitAudio();
        }

        public void SetMuteWhenUnfocused(bool muted)
        {
            Settings.Audio.MuteWhenUnfocused = muted;
            CommitAudio();
        }

        /// <summary>The Audio tab's Reset to defaults (Decision 13).</summary>
        public void ResetAudio()
        {
            Settings.Audio = new PlayerAudioSettings();
            CommitAudio();
        }

        /// <summary>Seconds left on the "Keep these settings?" prompt, or 0 when nothing awaits confirmation.</summary>
        public float DisplayConfirmationSecondsLeft => _windowConfirmation.SecondsLeft(Time.unscaledTime);

        public bool DisplayConfirmationPending => _windowConfirmation.IsPending;

        /// <summary>Sizes the Resolution dropdown offers, largest first.</summary>
        public Vector2Int[] ResolutionChoices => PlayerDisplayApplier.ResolutionChoices();

        /// <summary>The resolution in effect: the saved one if the display offers it, else native.</summary>
        public Vector2Int CurrentResolution => PlayerDisplayApplier.CurrentResolution(Settings.Display);

        public bool StpSupported => PlayerDisplayApplier.StpSupported;

        public void SetResolution(Vector2Int size) => ChangeWindow(d =>
        {
            d.ResolutionWidth = size.x;
            d.ResolutionHeight = size.y;
        });

        public void SetWindowMode(PlayerWindowMode mode) => ChangeWindow(d => d.WindowMode = mode);

        /// <summary>Keeps a resolution/window change; without this it reverts after 15 s (Decision 3).</summary>
        public void ConfirmDisplayChange()
        {
            _windowConfirmation.Confirm();
            ScheduleSave();
            Changed?.Invoke();
        }

        public void RevertDisplayChange()
        {
            if (!_windowConfirmation.IsPending) return;
            _windowConfirmation.Previous.WriteTo(Settings.Display);
            _windowConfirmation.Confirm();
            _display.ApplyWindow(Settings.Display);
            ScheduleSave();
            Changed?.Invoke();
        }

        public void SetVSync(bool on) => ChangeQuality(d => d.VSync = on);

        public void SetFrameCap(int cap) => ChangeQuality(d => d.FrameCap = cap);

        public void SetQuality(PlayerQualityTier tier) => ChangeQuality(d => d.Quality = tier);

        public void SetRenderScale(float scale) => ChangeQuality(d => d.RenderScale = scale);

        public void SetUpscaler(PlayerUpscaler upscaler) => ChangeQuality(d => d.Upscaler = upscaler);

        public void SetAntiAliasing(PlayerAntiAliasing antiAliasing) => ChangeQuality(d => d.AntiAliasing = antiAliasing);

        /// <summary>Display-only overlay toggle; nothing to apply to the engine, the FPS view reads it on Changed.</summary>
        public void SetShowFps(bool on)
        {
            Settings.Display.ShowFps = on;
            ScheduleSave();
            Changed?.Invoke();
        }

        /// <summary>Display-only overlay toggle; LogIndicatorView applies it to the console.</summary>
        public void SetShowLogIndicator(bool on)
        {
            Settings.Display.ShowLogIndicator = on;
            ScheduleSave();
            Changed?.Invoke();
        }

        /// <summary>Called on slider release, not per drag step (Decision 8).</summary>
        public void SetUiScale(float scale)
        {
            Settings.Display.UiScale = PlayerDisplaySettings.SnapUiScale(scale);
            _display.ApplyUiScale(Settings.Display.UiScale);
            ScheduleSave();
            Changed?.Invoke();
        }

        /// <summary>The Display tab's Reset to defaults. A changed resolution or window mode still asks to be kept.</summary>
        public void ResetDisplay()
        {
            WindowState before = new(Settings.Display);
            Settings.Display = new PlayerDisplaySettings();
            if (!before.Equals(new WindowState(Settings.Display))) _windowConfirmation.Begin(before, Time.unscaledTime);
            _display.ApplyAll(Settings.Display);
            ScheduleSave();
            Changed?.Invoke();
        }

        /// <summary>
        /// Screen-space UI panels register here so UI Scale reaches them (Decision 8). Shared assets are scaled once.
        /// </summary>
        public void RegisterUiPanel(PanelSettings panel)
        {
            if (_active) _display.RegisterPanel(panel);
        }

        private void ChangeWindow(Action<PlayerDisplaySettings> change)
        {
            WindowState before = new(Settings.Display);
            change(Settings.Display);
            Settings.Display.Sanitize();
            if (before.Equals(new WindowState(Settings.Display))) return;
            _windowConfirmation.Begin(before, Time.unscaledTime);
            _display.ApplyWindow(Settings.Display);
            ScheduleSave();
            Changed?.Invoke();
        }

        private void ChangeQuality(Action<PlayerDisplaySettings> change)
        {
            change(Settings.Display);
            Settings.Display.Sanitize();
            _display.ApplyQuality(Settings.Display);
            ScheduleSave();
            Changed?.Invoke();
        }

        private IDisposable _capture;

        /// <summary>Row id waiting for its next key, or null.</summary>
        public string CapturingRowId { get; private set; }

        /// <summary>The last rebind outcome worth telling the player ("Use is now Q"), or null.</summary>
        public string ControlsNotice { get; private set; }

        /// <summary>Bumps with every new <see cref="ControlsNotice"/>, so a view shows each one once.</summary>
        public int ControlsNoticeVersion { get; private set; }

        /// <summary>The Controls tab's rows; empty before InputSubSystem exists.</summary>
        public IReadOnlyList<KeybindRow> KeybindRows =>
            SubSystems.TryGet(out InputSubSystem input) ? input.BuildKeybindRows() : Array.Empty<KeybindRow>();

        /// <summary>Starts listening for the row's new key; Escape (or <see cref="CancelKeyCapture"/>) cancels.</summary>
        public void BeginKeyCapture(string rowId)
        {
            if (!_active || !SubSystems.TryGet(out InputSubSystem input)) return;
            _capture?.Dispose();
            CapturingRowId = rowId;
            ControlsNotice = null;
            _capture = input.CaptureNextButton(path =>
            {
                _capture = null;
                CapturingRowId = null;
                KeybindResult result = input.Rebind(rowId, path);
                ControlsNotice = result.Notice;
                if (result.Notice != null) ControlsNoticeVersion++;
                if (result.Accepted) StoreBindings(input);
                Changed?.Invoke();
            }, () =>
            {
                _capture = null;
                CapturingRowId = null;
                Changed?.Invoke();
            });
            Changed?.Invoke();
        }

        public void CancelKeyCapture()
        {
            if (_capture == null) return;
            _capture.Dispose();
            _capture = null;
            CapturingRowId = null;
            Changed?.Invoke();
        }

        /// <summary>The Controls tab's Reset to defaults: every binding.</summary>
        public void ResetControls()
        {
            CancelKeyCapture();
            Settings.Controls = new PlayerControlsSettings();
            ControlsNotice = null;
            ApplyControls();
            ScheduleSave();
            Changed?.Invoke();
        }

        private void ApplyControls()
        {
            if (!_active || !SubSystems.TryGet(out InputSubSystem input)) return;
            input.LoadBindingOverrides(Settings.Controls.BindingOverrides, Settings.Controls.SystemBindingOverrides);
        }

        private void StoreBindings(InputSubSystem input)
        {
            Settings.Controls.BindingOverrides = input.SaveBindingOverrides();
            Settings.Controls.SystemBindingOverrides = input.SaveSystemBindingOverrides();
            ScheduleSave();
        }

        /// <summary>Writes pending changes now instead of after the debounce (quit, pilots).</summary>
        public bool SaveNow()
        {
            _saveDueTime = -1f;
            if (_store == null) return false;
            if (!_windowConfirmation.IsPending) return _store.Save(Settings);
            // An unconfirmed resolution must not survive a crash: write the last confirmed one instead.
            PlayerSettings onDisk = JsonUtility.FromJson<PlayerSettings>(JsonUtility.ToJson(Settings));
            _windowConfirmation.Previous.WriteTo(onDisk.Display);
            return _store.Save(onDisk);
        }

        private void CommitAudio()
        {
            ApplyAudio();
            ScheduleSave();
            Changed?.Invoke();
        }

        private void ScheduleSave()
        {
            if (_active) _saveDueTime = Time.unscaledTime + SaveDebounceSeconds;
        }

        private void ApplyAudio()
        {
            if (!_active) return;
            AudioMixer mixer = MainAudioMixer.Mixer;
            if (mixer == null)
            {
                Debug.LogWarning("[Settings] MainMixer not found in Resources; volume settings are not applied.");
                return;
            }

            PlayerAudioSettings audio = Settings.Audio;
            mixer.SetFloat(MainAudioMixer.MasterVolume, PlayerAudioMix.MasterDecibels(audio, _focused));
            // Mutes also cover any AudioSource that bypasses the mixer (no output group set).
            AudioListener.volume = PlayerAudioMix.IsMuted(audio, _focused) ? 0f : 1f;
            mixer.SetFloat(MainAudioMixer.MusicVolume, PlayerAudioMix.ToDecibels(audio.Music));
            mixer.SetFloat(MainAudioMixer.SfxVolume, PlayerAudioMix.ToDecibels(audio.Sfx));
            mixer.SetFloat(MainAudioMixer.AmbienceVolume, PlayerAudioMix.ToDecibels(audio.Ambience));
            mixer.SetFloat(MainAudioMixer.PersonalVolume, PlayerAudioMix.ToDecibels(audio.Personal));
        }

        private void HandleFocusChanged(bool focused)
        {
            _focused = focused;
            ApplyAudio();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_windowConfirmation.ShouldRevert(Time.unscaledTime)) RevertDisplayChange();
            if (_saveDueTime >= 0f && Time.unscaledTime >= _saveDueTime) SaveNow();
        }
    }
}
