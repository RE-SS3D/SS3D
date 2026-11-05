using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.ScreenEffects;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps synced <see cref="HealthEnvironmentState"/> onto temperature/fire
    /// <see cref="ScreenEffectType"/> intensities. Leaves health-driven types alone.
    /// </summary>
    public static class AtmosScreenEffectMapper
    {
        public static void Apply(HealthEnvironmentState env, ScreenEffectsSubSystem effects)
        {
            if (effects == null || effects.IsDebugOverrideActive)
            {
                return;
            }

            if (HealthEnvironmentSettings.AtmosphericDamageDisabled || !env.HasSample)
            {
                Clear(effects);
                return;
            }

            float hot = 0f;
            float cold = 0f;
            float freezing = 0f;
            float fire = 0f;

            if (env.TemperatureKelvin >= AirAlarmConstants.HighTemperatureKelvin)
            {
                hot = Mathf.Clamp01(Mathf.InverseLerp(
                    AirAlarmConstants.HighTemperatureKelvin,
                    AirAlarmConstants.HighTemperatureKelvin + 40f,
                    env.TemperatureKelvin));
            }

            if (!env.IsSealed && env.TemperatureKelvin <= HealthConstants.ColdWarningTemperatureKelvin)
            {
                cold = Mathf.Clamp01(Mathf.InverseLerp(
                    HealthConstants.ColdWarningTemperatureKelvin,
                    HealthConstants.ColdDamageTemperatureKelvin,
                    env.TemperatureKelvin));
            }

            if (!env.IsSealed && env.TemperatureKelvin <= HealthConstants.FreezingTemperatureKelvin)
            {
                freezing = Mathf.Clamp01(Mathf.InverseLerp(
                    HealthConstants.ColdDamageTemperatureKelvin,
                    HealthConstants.FreezingTemperatureKelvin - 20f,
                    env.TemperatureKelvin));
            }

            if (env.BurnIntensity > 0f)
            {
                fire = Mathf.Clamp01(env.BurnIntensity);
            }

            effects.SetEffect(ScreenEffectType.HotRoom, hot);
            effects.SetEffect(ScreenEffectType.ColdRoom, cold);
            effects.SetEffect(ScreenEffectType.Freezing, freezing);
            effects.SetEffect(ScreenEffectType.OnFire, fire);
        }

        public static void Clear(ScreenEffectsSubSystem effects)
        {
            if (effects == null || effects.IsDebugOverrideActive)
            {
                return;
            }

            effects.SetEffect(ScreenEffectType.HotRoom, 0f);
            effects.SetEffect(ScreenEffectType.ColdRoom, 0f);
            effects.SetEffect(ScreenEffectType.Freezing, 0f);
            effects.SetEffect(ScreenEffectType.OnFire, 0f);
        }
    }
}
