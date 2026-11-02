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
        [SerializeField] private Texture2D[] _wallpapers;
        [SerializeField] private Texture2D _musicIcon;
        [SerializeField] private AudioClip[] _music;
        public VisualTreeAsset Template => _template;
        public StyleSheet Style => _style;
        public Texture2D Logo => _logo;
        public int WallpaperCount => _wallpapers?.Length ?? 0;
        public Texture2D MusicIcon => _musicIcon;
        /// <summary>Streamed menu tracks; may be empty (the music row then shows "No music").</summary>
        public AudioClip[] Music => _music ?? System.Array.Empty<AudioClip>();
        public Texture2D Wallpaper(int index) => _wallpapers[index % _wallpapers.Length];
        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = null;
            if (_template == null) missingField = nameof(_template);
            else if (_style == null) missingField = nameof(_style);
            else if (_logo == null) missingField = nameof(_logo);
            else if (_wallpapers == null || _wallpapers.Length == 0) missingField = nameof(_wallpapers);
            else if (_musicIcon == null) missingField = nameof(_musicIcon);
            if (missingField == null)
                foreach (Texture2D texture in _wallpapers)
                    if (texture == null) missingField = nameof(_wallpapers);
            return missingField == null;
        }
#if UNITY_EDITOR
        public void EditorAssign(PanelSettings panel, StyleSheet tokens, StyleSheet typography,
            VisualTreeAsset template, StyleSheet style, Texture2D logo, Texture2D[] wallpapers,
            Texture2D musicIcon, AudioClip[] music)
        {
            EditorAssignShared(panel, tokens, typography);
            _template = template;
            _style = style;
            _logo = logo;
            _wallpapers = wallpapers;
            _musicIcon = musicIcon;
            _music = music;
        }
#endif
    }
}
