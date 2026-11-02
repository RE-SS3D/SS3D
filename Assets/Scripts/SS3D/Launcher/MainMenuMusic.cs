using System.Collections.Generic;
using System.Text;
using SS3D.Systems.Audio;
using UnityEngine;

namespace SS3D.Launcher
{
    /// <summary>
    /// Main-menu music: one 2D source on the mixer's Music group (so the player's Music and Master
    /// volumes apply), a random first track, then the next one when a track ends. Fades in over
    /// 0.9 s and out over 0.5 s, pausing at silence so a track resumes where it stopped.
    /// Call <see cref="Tick"/> every frame.
    /// </summary>
    public sealed class MainMenuMusic
    {
        private const float FadeInSeconds = 0.9f;
        private const float FadeOutSeconds = 0.5f;
        private static readonly HashSet<string> SmallWords = new() { "a", "an", "and", "of", "the", "to", "in", "on" };
        private static readonly Dictionary<string, string> KnownTitles = new()
        {
            { "SpacemansLullaby", "Spaceman's Lullaby" },
        };

        private readonly AudioSource _source;
        private readonly AudioClip[] _tracks;
        private int _track;
        private bool _audible;

        public MainMenuMusic(GameObject host, AudioClip[] tracks)
        {
            _tracks = tracks ?? System.Array.Empty<AudioClip>();
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0;
            _source.volume = 0;
            _source.ignoreListenerPause = true;
            _source.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.MusicGroup);
            _track = _tracks.Length > 0 ? Random.Range(0, _tracks.Length) : 0;
            if (_tracks.Length > 0)
            {
                _source.clip = _tracks[_track];
            }
        }

        public bool HasTracks => _tracks.Length > 0;

        public string Title => HasTracks ? TitleOf(_tracks[_track].name) : "";

        /// <summary>Fade towards playing (true) or silence (false).</summary>
        public void SetAudible(bool audible)
        {
            _audible = audible && HasTracks;
            if (_audible && !_source.isPlaying)
            {
                if (_source.time > 0)
                {
                    _source.UnPause();
                }
                else
                {
                    _source.Play();
                }
            }
        }

        public void Next()
        {
            if (!HasTracks)
            {
                return;
            }

            _track = (_track + 1) % _tracks.Length;
            _source.Stop();
            _source.clip = _tracks[_track];
            _source.time = 0;
            _source.volume = 0;
            SetAudible(_audible);
        }

        /// <summary>Advances fades; returns true when the track changed (the title must be re-presented).</summary>
        public bool Tick()
        {
            float target = _audible ? 1f : 0f;
            float rate = Time.unscaledDeltaTime / (target > _source.volume ? FadeInSeconds : FadeOutSeconds);
            _source.volume = Mathf.MoveTowards(_source.volume, target, rate);
            if (!_audible && _source.volume <= 0f && _source.isPlaying)
            {
                _source.Pause();
            }

            // Finished on its own (not paused by a fade): play the next track.
            if (_audible && !_source.isPlaying && _source.clip != null && _source.time <= 0f)
            {
                Next();
                return true;
            }

            return false;
        }

        public void Stop()
        {
            _audible = false;
            _source.Stop();
            _source.volume = 0;
        }

        /// <summary>"GatewayToTheForest" → "Gateway to the Forest"; "Title1" → "Title 1".</summary>
        public static string TitleOf(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return "";
            }

            if (KnownTitles.TryGetValue(clipName, out string known))
            {
                return known;
            }

            List<string> words = new();
            StringBuilder word = new();
            for (int i = 0; i < clipName.Length; i++)
            {
                char c = clipName[i];
                bool boundary = i > 0 && (char.IsUpper(c) || (char.IsDigit(c) && !char.IsDigit(clipName[i - 1])));
                if (c is ' ' or '_' or '-' || boundary)
                {
                    if (word.Length > 0)
                    {
                        words.Add(word.ToString());
                    }

                    word.Clear();
                    if (c is ' ' or '_' or '-')
                    {
                        continue;
                    }
                }

                word.Append(c);
            }

            if (word.Length > 0)
            {
                words.Add(word.ToString());
            }

            for (int i = 1; i < words.Count - 1; i++)
            {
                string lower = words[i].ToLowerInvariant();
                if (SmallWords.Contains(lower))
                {
                    words[i] = lower;
                }
            }

            return string.Join(" ", words);
        }
    }
}
