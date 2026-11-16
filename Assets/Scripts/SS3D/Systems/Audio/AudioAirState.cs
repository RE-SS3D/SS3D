using SS3D.Systems.Atmospherics.Visualization;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// How the air at the listener shapes what they hear (audio.md §2, §3). Pure, so the muffle curve
    /// is testable without a live scene, like <see cref="AudioOcclusionState"/>.
    /// </summary>
    public static class AudioAirState
    {
        public const int WindLayerCount = 3;

        /// <summary>At or above this, air carries sound in full.</summary>
        public const float FullPressureKpa = 70f;

        /// <summary>At or below this, nothing carries. Matches where the simulation settles a cell to zero.</summary>
        public const float SilentPressureKpa = 5f;

        /// <summary>Hearing left for something within a tile, "like a sound felt through a suit".</summary>
        public const float TouchHearing = 0.25f;

        private const float TouchNearDistance = 1f;
        private const float TouchFarDistance = 1.75f;
        private const float SilentCutoffHz = 300f;

        public static float AirFromPressure(float pressureKpa) =>
            Mathf.InverseLerp(SilentPressureKpa, FullPressureKpa, pressureKpa);

        /// <summary>
        /// The air the listener should settle toward. A wall tile holds no gas, so the last reading
        /// stands. No data is air, unless the server has said the body is in open space: a late joiner
        /// holds no chunk for a room that hasn't changed, and that must not sound like vacuum.
        /// </summary>
        public static float ResolveAir(AtmosCellVisualEncoding.CellRead read, float pressureKpa, float currentAir,
            bool inOpenSpace)
        {
            return read switch
            {
                AtmosCellVisualEncoding.CellRead.Open => AirFromPressure(pressureKpa),
                AtmosCellVisualEncoding.CellRead.Blocked => currentAir,
                _ => inOpenSpace ? 0f : 1f,
            };
        }

        /// <summary>How well a source this far from the listener is heard, 0..1. Also its volume scale.</summary>
        public static float Hearing(float air, float distance) =>
            Mathf.Max(air, TouchHearing * (1f - Mathf.InverseLerp(TouchNearDistance, TouchFarDistance, distance)));

        /// <summary>Low-pass cutoff for a hearing level, even in octaves, so the highs go first.</summary>
        public static float CutoffHz(float hearing) =>
            SilentCutoffHz * Mathf.Pow(AudioOcclusionState.ClearCutoffHz / SilentCutoffHz, Mathf.Clamp01(hearing));

        /// <summary>
        /// One layer's share of the wind at this speed in m/s. The clips are mastered far apart, about
        /// 18 dB from light to violent, so a rising wind gets louder by moving up the layers.
        /// </summary>
        public static float WindGain(int layer, float windSpeed)
        {
            switch (layer)
            {
                case 0:
                    return Rise(0.2f, 1.5f, windSpeed) * (1f - Rise(2.5f, 4f, windSpeed));
                case 1:
                    return Rise(1.5f, 3f, windSpeed) * (1f - Rise(4f, 5f, windSpeed));
                case 2:
                    return Rise(3.5f, 5f, windSpeed);
                default:
                    return 0f;
            }
        }

        private static float Rise(float from, float to, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    }
}
