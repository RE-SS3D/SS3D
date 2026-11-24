using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace SS3D.UI.Shell.MainMenu
{
    public sealed class MainMenuAssetCatalog : UiAssetCatalogBase
    {
        [SerializeField] private VisualTreeAsset _template;
        [SerializeField] private StyleSheet _style;
        [SerializeField] private Texture2D _logo;
        /// <summary>Lazy: a launch shows one of dozens, and this catalog sits in Resources.</summary>
        [SerializeField] private LazyLoadReference<Texture2D>[] _wallpapers;
        [SerializeField] private VideoClip[] _wallpaperVideos;
        [SerializeField] private Texture2D _musicIcon;
        [SerializeField] private AudioClip[] _music;
        public VisualTreeAsset Template => _template;
        public StyleSheet Style => _style;
        public Texture2D Logo => _logo;
        /// <summary>Share of launches that get a video; a flat pick would bury the few clips among the stills.</summary>
        private const float VideoShare = 0.5f;
        private int StillCount => _wallpapers?.Length ?? 0;
        private int VideoCount => _wallpaperVideos?.Length ?? 0;
        /// <summary>Wallpaper indices run through the stills, then the videos.</summary>
        public int WallpaperCount => StillCount + VideoCount;
        public Texture2D MusicIcon => _musicIcon;
        /// <summary>Streamed menu tracks; may be empty (the music row then shows "No music").</summary>
        public AudioClip[] Music => _music ?? System.Array.Empty<AudioClip>();
        /// <summary>The still for an index. A video's index wraps onto a still, shown until its first frame or if it fails.</summary>
        public Texture2D Wallpaper(int index) => _wallpapers[index % _wallpapers.Length].asset;
        /// <summary>The video for an index, or null when the index is a still.</summary>
        public VideoClip WallpaperVideo(int index) =>
            index >= StillCount && index - StillCount < VideoCount ? _wallpaperVideos[index - StillCount] : null;
        /// <summary>A wallpaper index from two rolls in [0, 1]: one for video or still, one for which of them.</summary>
        public int PickWallpaper(float kindRoll, float itemRoll)
        {
            bool video = VideoCount > 0 && kindRoll < VideoShare;
            int count = video ? VideoCount : StillCount;
            int item = Mathf.Min((int)(itemRoll * count), count - 1);
            return video ? StillCount + item : item;
        }
        protected override bool HasSurfaceAssets(out string missingField)
        {
            missingField = null;
            if (_template == null) missingField = nameof(_template);
            else if (_style == null) missingField = nameof(_style);
            else if (_logo == null) missingField = nameof(_logo);
            else if (_wallpapers == null || _wallpapers.Length == 0) missingField = nameof(_wallpapers);
            else if (_musicIcon == null) missingField = nameof(_musicIcon);
            if (missingField == null)
                foreach (LazyLoadReference<Texture2D> texture in _wallpapers)
                    if (!texture.isSet) missingField = nameof(_wallpapers); // Not .asset: that would load them all.
            if (missingField == null && _wallpaperVideos != null)
                foreach (VideoClip video in _wallpaperVideos)
                    if (video == null) missingField = nameof(_wallpaperVideos);
            return missingField == null;
        }
#if UNITY_EDITOR
        public void EditorAssign(PanelSettings panel, StyleSheet tokens, StyleSheet typography,
            VisualTreeAsset template, StyleSheet style, Texture2D logo, Texture2D[] wallpapers,
            VideoClip[] wallpaperVideos, Texture2D musicIcon, AudioClip[] music)
        {
            EditorAssignShared(panel, tokens, typography);
            _template = template;
            _style = style;
            _logo = logo;
            _wallpapers = System.Array.ConvertAll(wallpapers, texture => new LazyLoadReference<Texture2D>(texture));
            _wallpaperVideos = wallpaperVideos;
            _musicIcon = musicIcon;
            _music = music;
        }
#endif
    }
}
