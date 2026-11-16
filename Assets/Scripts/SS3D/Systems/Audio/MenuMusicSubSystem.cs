using System;
using SS3D.Core.Behaviours;
using SS3D.UI.Shell.MainMenu;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Menu music for the whole process (lobby effort Decision 15). Created by <c>SystemsBootstrap</c> on clients, it
    /// outlives every scene, so the music plays on from the main menu through the session load into the lobby. The
    /// main menu and the lobby say whether their screen wants music (<see cref="MenuMusicDemand"/>). It plays when
    /// one does and the player's Menu music setting is on, and fades when the player takes a body or observes.
    /// Tracks come from the main-menu catalog.
    /// </summary>
    public sealed class MenuMusicSubSystem : SubSystem
    {
        private readonly MenuMusicDemand _demand = new();
        private MenuMusicPlayer _player;
        private bool _unsupported;
        private bool _audible;

        /// <summary>The track changed, on its own or by <see cref="Next"/>: a shown title is stale.</summary>
        public event Action TrackChanged;

        public bool HasTracks => _player != null && _player.HasTracks;

        public string Title => _player?.Title ?? "";

        /// <summary>
        /// The player's Menu music setting. <c>PlayerSettingsSubSystem</c> pushes it here whenever it applies audio, so
        /// Audio needs no reference to Settings; change it with <c>PlayerSettingsSubSystem.SetMenuMusic</c>.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <inheritdoc cref="MenuMusicDemand.SetMainMenu"/>
        public void SetMainMenu(bool wanted) => _demand.SetMainMenu(wanted);

        /// <inheritdoc cref="MenuMusicDemand.HandOverToLobby"/>
        public void HandOverToLobby() => _demand.HandOverToLobby(Time.unscaledTime);

        /// <inheritdoc cref="MenuMusicDemand.SetLobby"/>
        public void SetLobby(bool? inLobby) => _demand.SetLobby(inLobby);

        /// <summary>A fresh main-menu visit starts the current track from the beginning.</summary>
        public void Restart()
        {
            _player?.Stop();
            _audible = false;
        }

        public void Next()
        {
            if (_player == null) return;
            _player.Next();
            TrackChanged?.Invoke();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _unsupported = UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }

        private void Update()
        {
            if (_unsupported) return;
            if (_player == null && !TryCreatePlayer()) return;
            bool audible = Enabled && _demand.Wanted(Time.unscaledTime);

            // Only on a change: SetAudible on a finished track would replay it before Tick moves to the next one.
            if (audible != _audible)
            {
                _audible = audible;
                _player.SetAudible(audible);
            }

            if (_player.Tick()) TrackChanged?.Invoke();
        }

        private bool TryCreatePlayer()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            if (catalog == null)
            {
                Debug.LogWarning("[MenuMusic] No main-menu catalog in Resources: menu music is off for this run.");
                _unsupported = true;
                return false;
            }

            _player = new MenuMusicPlayer(gameObject, catalog.Music);
            // The main menu may have shown before the first track was picked.
            TrackChanged?.Invoke();
            return true;
        }
    }
}
