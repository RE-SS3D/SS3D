using UnityEngine;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Tunables for local speech subtitles: distance/occlusion tiers, crowd cap, stacking, drift,
    /// and fade timing. A plain feature-local ScriptableObject (referenced directly by serialized
    /// fields), not a ScriptableSettings singleton like CommsChannels - this config only matters to
    /// this one controller, so there's no need for the global project-settings lookup.
    /// </summary>
    [CreateAssetMenu(fileName = "New Local Speech Config", menuName = "SS3D/UI/Comms/Local Speech Config")]
    public class LocalSpeechConfig : ScriptableObject
    {
        [Header("Distance / occlusion tiers - comms.md §3")]
        [Tooltip("Within this distance, unobstructed, speech renders as Clear.")]
        public float ClearDistance = 5f;

        [Tooltip("Beyond this distance, speech is Inaudible regardless of occlusion.")]
        public float InaudibleDistance = 10f;

        [Tooltip("If blocked by solid geometry but within this distance, speech degrades to Muffled instead of Inaudible.")]
        public float GraceDistance = 5f;

        [Tooltip("Layer mask used for the occlusion raycast between viewer and speaker. Should contain solid geometry (walls) only.")]
        public LayerMask OcclusionMask = 1; // Default layer, matching DropInteraction's line-of-sight check.

        [Header("Crowd cap - comms.md §4")]
        [Tooltip("Maximum number of speakers shown at once to a single viewer. The rest compress into an overflow chip.")]
        public int MaxVisibleBubbles = 3;

        [Tooltip("Seconds between periodic crowd-cap re-ranks, to catch movement-driven tier/rank changes.")]
        public float RerankIntervalSeconds = 0.25f;

        [Header("Per-speaker stack")]
        [Tooltip("Max simultaneous subtitle lines stacked above one speaker. Older lines drop off.")]
        public int MaxStackedMessagesPerSpeaker = 3;

        [Tooltip("Vertical gap in screen pixels between stacked lines (newest nearest the head).")]
        public float StackSpacingPixels = 48f;

        [Tooltip("Upward drift speed in screen pixels per second as a line ages.")]
        public float DriftPixelsPerSecond = 14f;

        [Header("Fade timing - comms.md §3")]
        [Tooltip("Base seconds a line stays visible before the fade-out tail. Short lines land near ~4s.")]
        public float BaseFadeDuration = 4f;
        public float PerCharacterFadeSeconds = 0.02f;
        public float MaxFadeDuration = 8f;

        [Header("Action lines - action-messages.md §5")]
        [Tooltip("Seconds an action chip stays up: quicker than speech so it never reads as talk.")]
        public float ActionFadeDuration = 2.5f;

        public float GetFadeDuration(int characterCount)
        {
            float duration = BaseFadeDuration + characterCount * PerCharacterFadeSeconds;
            return Mathf.Min(duration, MaxFadeDuration);
        }
    }
}
