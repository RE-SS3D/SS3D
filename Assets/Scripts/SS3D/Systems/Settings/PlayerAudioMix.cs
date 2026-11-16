using UnityEngine;

namespace SS3D.Systems.Settings
{
    /// <summary>Slider levels → MainMixer decibels. Pure, so the mapping is testable without an AudioMixer.</summary>
    public static class PlayerAudioMix
    {
        /// <summary>The mixer's floor; a 0 slider maps here rather than to −∞ (2026-10_player-settings Phase 1).</summary>
        public const float SilentDecibels = -80f;

        public static float ToDecibels(float linear)
        {
            if (linear <= 0f || float.IsNaN(linear)) return SilentDecibels;
            return Mathf.Max(SilentDecibels, 20f * Mathf.Log10(Mathf.Min(linear, 1f)));
        }

        /// <summary>Mute All, or Mute-when-unfocused while the window lacks focus (Decision 4).</summary>
        public static bool IsMuted(PlayerAudioSettings audio, bool applicationFocused) =>
            audio.MuteAll || (audio.MuteWhenUnfocused && !applicationFocused);

        /// <summary>Master after the mutes.</summary>
        public static float MasterDecibels(PlayerAudioSettings audio, bool applicationFocused) =>
            IsMuted(audio, applicationFocused) ? SilentDecibels : ToDecibels(audio.Master);
    }
}
