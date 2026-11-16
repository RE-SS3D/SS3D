using UnityEngine;
using UnityEngine.Audio;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Runtime handle on <c>Assets/Content/Systems/Audio/Resources/MainMixer.mixer</c>, for code-built
    /// <see cref="AudioSource"/>s that have no prefab to carry their output group, and for player volume settings.
    /// </summary>
    public static class MainAudioMixer
    {
        public const string MasterGroup = "Master";
        public const string MusicGroup = "Music";
        public const string SfxGroup = "SFX";
        public const string AmbienceGroup = "Ambience";
        public const string PersonalGroup = "Personal";

        // Exposed group volumes in dB, driven by PlayerSettingsSubSystem (audio.md §7).
        public const string MasterVolume = "MasterVolume";
        public const string MusicVolume = "MusicVolume";
        public const string SfxVolume = "SFXVolume";
        public const string AmbienceVolume = "AmbienceVolume";
        public const string PersonalVolume = "PersonalVolume";

        private static AudioMixer _mixer;

        public static AudioMixer Mixer => _mixer != null ? _mixer : _mixer = Resources.Load<AudioMixer>("MainMixer");

        /// <summary>The group with exactly this name, or null (the source then plays straight to the listener).</summary>
        public static AudioMixerGroup FindGroup(string name)
        {
            if (Mixer == null) return null;
            foreach (AudioMixerGroup group in Mixer.FindMatchingGroups(string.Empty))
                if (group.name == name) return group;
            return null;
        }
    }
}
