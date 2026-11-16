using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Pure occlusion→cutoff/volume mapping for a positional audio source (audio.md §3).
    /// Kept separate from <see cref="AudioSourceOcclusion"/> so the muffle curve is testable
    /// without a live scene, mirroring how AccuracyCone/RangedWeaponProfile stay pure.
    /// </summary>
    public static class AudioOcclusionState
    {
        public const float ClearCutoffHz = 22000f;
        public const float OccludedCutoffHz = 800f;
        public const float OccludedVolumeScale = 0.5f;

        /// <summary>
        /// Lerps the low-pass cutoff toward the occluded or clear target. <paramref name="lerpFactor"/>
        /// is expected to be <c>Time.deltaTime * lerpSpeed</c>, clamped by <see cref="Mathf.Lerp"/> itself.
        /// </summary>
        public static float LerpCutoffHz(float currentHz, bool occluded, float lerpFactor)
        {
            return Mathf.Lerp(currentHz, occluded ? OccludedCutoffHz : ClearCutoffHz, lerpFactor);
        }

        /// <summary>
        /// Lerps the volume scale (multiplied onto the source's intended volume) toward the
        /// occluded or clear target.
        /// </summary>
        public static float LerpVolumeScale(float currentScale, bool occluded, float lerpFactor)
        {
            return Mathf.Lerp(currentScale, occluded ? OccludedVolumeScale : 1f, lerpFactor);
        }
    }
}
