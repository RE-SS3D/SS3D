using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Decides whether the atlas holds anything the fullscreen atmos passes could draw. Plain room air
    /// changes no pixel, yet the three passes march every pixel, so a calm atlas lets them be skipped.
    /// </summary>
    public static class AtmosVisualActivity
    {
        // These mirror the early-outs in AtmosCommon.hlsl: lower them if the shaders start reacting sooner.
        private const float VisibleFire = 0.001f;
        private const float VisibleProfile = 0.001f;
        private const float DistortionTemperature = 294.15f;

        public static bool IsCalm(float[] temperature, Color32[] composition, float[] fire, GasVisualProfileBuilder.GpuSet profiles)
        {
            if (profiles.Scatter == null || profiles.Emission == null || profiles.Misc == null) return false;

            for (int i = 0; i < fire.Length; i++)
            {
                if (fire[i] > VisibleFire || temperature[i] >= DistortionTemperature) return false;
                Color32 gas = composition[i];
                if (IsVisible(gas.r, 0, profiles) || IsVisible(gas.g, 1, profiles) || IsVisible(gas.b, 2, profiles) || IsVisible(gas.a, 3, profiles)) return false;
            }

            return true;
        }

        private static bool IsVisible(byte fraction, int channel, GasVisualProfileBuilder.GpuSet profiles)
        {
            return fraction > 0
                && (profiles.Scatter[channel].w > VisibleProfile
                    || profiles.Emission[channel].w > VisibleProfile
                    || profiles.Misc[channel].x > VisibleProfile);
        }
    }
}
