using SS3D.Core;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.ScreenEffects
{
    /// <summary>
    /// Client: what everyone sees when the station's device detonates (effort <c>2026-11_round-end-loop</c>
    /// Decision 19): a white-out and a heavy camera shake, then the round-end summary. Reuses the blast
    /// pipeline's pieces (<see cref="ScreenEffectsSubSystem"/>, <see cref="CameraFollow.AddImpulse"/>) at
    /// station scale; no blast is resolved on the station.
    /// </summary>
    public static class StationDetonationPresentation
    {
        /// <summary>White-out length; the summary waits this long before it opens.</summary>
        public const float Seconds = 2.5f;

        private const float ShakeAmplitude = 0.6f;
        private const float ShakeSeconds = 2f;

        private static float _playedAt = float.NegativeInfinity;

        /// <summary>The white-out is still on screen.</summary>
        public static bool IsPlaying => Time.unscaledTime - _playedAt < Seconds;

        // Play mode without a domain reload restarts the clock; an old timestamp would hold the summary back.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _playedAt = float.NegativeInfinity;

        public static void Play()
        {
            _playedAt = Time.unscaledTime;
            if (SubSystems.TryGet(out ScreenEffectsSubSystem screenEffects))
                screenEffects.TriggerWhiteout(Seconds);

            CameraFollow follow = Object.FindFirstObjectByType<CameraFollow>();
            if (follow != null)
                follow.AddImpulse(ShakeAmplitude, ShakeSeconds);
        }
    }
}
