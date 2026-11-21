using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.MainMenu
{
    public sealed class MainMenuAssetCatalog : UiAssetCatalogBase
    {
        [SerializeField] private VisualTreeAsset _template;
        [SerializeField] private StyleSheet _style;
        [SerializeField] private Texture2D _logo;
        [SerializeField] private Texture2D _wallpaper;
        [SerializeField] private Texture2D _musicIcon;
        [SerializeField] private AudioClip[] _music;
        public VisualTreeAsset Template => _template;
        public StyleSheet Style => _style;
        public Texture2D Logo => _logo;
        /// <summary>The menu's only wallpaper (<see cref="MainMenuAssetPaths.Wallpaper"/>).</summary>
        public Texture2D Wallpaper => _wallpaper;
        public Texture2D MusicIcon => _musicIcon;
        /// <summary>Streamed menu tracks; may be empty (the music row then shows "No music").</summary>
        public AudioClip[] Music => _music ?? System.Array.Empty<AudioClip>();
        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = null;
            if (_template == null) missingField = nameof(_template);
            else if (_style == null) missingField = nameof(_style);
            else if (_logo == null) missingField = nameof(_logo);
            else if (_wallpaper == null) missingField = nameof(_wallpaper);
            else if (_musicIcon == null) missingField = nameof(_musicIcon);
            return missingField == null;
        }
#if UNITY_EDITOR
        public void EditorAssign(PanelSettings panel, StyleSheet tokens, StyleSheet typography,
            VisualTreeAsset template, StyleSheet style, Texture2D logo, Texture2D wallpaper,
            Texture2D musicIcon, AudioClip[] music)
        {
            EditorAssignShared(panel, tokens, typography);
            _template = template;
            _style = style;
            _logo = logo;
            _wallpaper = wallpaper;
            _musicIcon = musicIcon;
            _music = music;
        }
#endif
    }
}
