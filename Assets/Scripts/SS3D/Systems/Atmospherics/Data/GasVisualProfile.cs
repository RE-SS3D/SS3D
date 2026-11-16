using System;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Authoring-time visual response for a gas in the GPU atmospherics passes.
    /// </summary>
    [Serializable]
    public struct GasVisualProfile
    {
        public Color ScatterColor;
        public float ScatterStrength;
        public Color EmissionColor;
        public float EmissionIntensity;
        public float DistortionScale;
        public float TurbulenceScale;

        public static GasVisualProfile Invisible =>
            new()
            {
                ScatterColor = Color.clear,
                ScatterStrength = 0f,
                EmissionColor = Color.clear,
                EmissionIntensity = 0f,
                DistortionScale = 0f,
                TurbulenceScale = 0f,
            };
    }
}
