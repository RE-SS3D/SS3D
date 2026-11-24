using SS3D.Logging;
using UnityEngine;
using UnityEngine.Video;

namespace SS3D.Launcher
{
    /// <summary>
    /// The looping, silent video behind the main menu, if the current wallpaper is one, decoded into a render texture the view shows.
    /// <see cref="Frame"/> stays null until a frame exists and after a decode error, so the menu keeps
    /// showing a still instead of black. Call <see cref="Tick"/> every frame.
    /// </summary>
    public sealed class MainMenuVideoWallpaper
    {
        private readonly VideoPlayer _player;
        private VideoClip _clip;
        private RenderTexture _target;
        private bool _hasFrame;
        private bool _presented;
        private bool _failed;

        public MainMenuVideoWallpaper(GameObject host)
        {
            _player = host.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.skipOnDrop = true;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.errorReceived += HandleError;
        }

        /// <summary>Stops the current clip and selects another; null means no video.</summary>
        public void Load(VideoClip clip)
        {
            Stop();
            _clip = clip;
            _failed = false;
            if (_player != null) _player.clip = clip;
            if (clip != null) Log.Information(this, "Main menu video wallpaper: {clip}", Logs.Important, clip.name);
        }

        /// <summary>The live frame, or null while there is none to show.</summary>
        public Texture Frame => _hasFrame ? _target : null;

        /// <summary>Play, or hold the current frame. Safe to call repeatedly.</summary>
        public void SetPlaying(bool playing)
        {
            if (_failed || _clip == null || _player == null)
            {
                return;
            }

            if (playing && !_player.isPlaying)
            {
                if (_target == null)
                {
                    _target = new RenderTexture((int)_clip.width, (int)_clip.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                    {
                        name = "Main menu video wallpaper",
                        hideFlags = HideFlags.HideAndDontSave,
                        wrapMode = TextureWrapMode.Clamp,
                    };
                    _player.targetTexture = _target;
                }

                _player.Play();
            }
            else if (!playing && _player.isPlaying)
            {
                _player.Pause();
            }
        }

        /// <summary>Stops decoding and frees the render texture; <see cref="SetPlaying"/> starts over.</summary>
        public void Stop()
        {
            _hasFrame = false;
            if (_player != null)
            {
                _player.Stop();
                _player.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
                _target = null;
            }
        }

        /// <summary>Returns true when <see cref="Frame"/> changed and the menu should present it.</summary>
        public bool Tick()
        {
            if (!_hasFrame && !_failed && _player != null && _player.isPlaying && _player.frame >= 0)
            {
                _hasFrame = true;
            }

            bool changed = _hasFrame != _presented;
            _presented = _hasFrame;
            return changed;
        }

        private void HandleError(VideoPlayer source, string message)
        {
            Log.Warning(this, $"Main menu video '{_clip.name}' failed, keeping the still: {message}", Logs.Important);
            _failed = true;
            Stop();
        }
    }
}
