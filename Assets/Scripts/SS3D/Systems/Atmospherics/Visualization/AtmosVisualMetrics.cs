using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// CPU-side estimates of the values the GPU visualization shaders consume.
    /// Useful for comparing live sim tiles against synthetic debug calibration.
    /// </summary>
    public struct AtmosCellVisualMetrics
    {
        public float TotalMoles;
        public float OxygenFraction;
        public float NitrogenFraction;
        public float CarbonDioxideFraction;
        public float PlasmaFraction;

        public float BurnIntensity;
        public float GlowTempFactor;
        public float DistortionTempFactor;
        public float GasFogDensity;
        public float Co2SmokeDriveFloor;
        public float Co2SmokeDriveMid;
        public float TemperatureGradient;
        public float DistortionWeightEstimate;
        public float PlasmaGlowEstimate;
    }

    public static class AtmosVisualMetrics
    {
        // Defaults mirrored from AtmosRendererFeature / GasVisualProfileBuilder.
        public const float ReferencePressureKpa = 101.325f;
        public const float ReferenceTemperatureK = 293.15f;
        public const float FogPressureScale = 80f;
        public const float StrongFireIntensity = 1f;
        public const float StrongFireBurnRateMolPerSec = 4f;
        public const float FireDistortionBoost = 3f;
        public const float HeatDistortionBlend = 0.35f;
        public const float VisualFireDecayPerTick = 0.96f;
        public const float GasFogSaturationPressureKpa = ReferencePressureKpa + FogPressureScale;

        public static AtmosCellVisualMetrics Compute(
            AtmosCellDebugInfo info,
            AtmosSimulation simulation,
            GasRegistry registry = null)
        {
            float o2 = simulation.DebugGetMoles(info.Coord, AtmosConstants.Oxygen);
            float n2 = simulation.DebugGetMoles(info.Coord, AtmosConstants.Nitrogen);
            float co2 = simulation.DebugGetMoles(info.Coord, AtmosConstants.CarbonDioxide);
            float plasma = simulation.DebugGetMoles(info.Coord, AtmosConstants.Plasma);
            float total = o2 + n2 + co2 + plasma;

            float o2Frac = SafeFraction(o2, total);
            float n2Frac = SafeFraction(n2, total);
            float co2Frac = SafeFraction(co2, total);
            float plasmaFrac = SafeFraction(plasma, total);

            float ignition = AtmosFluxConstants.PlasmaIgnitionTemperature;
            float glowTempFactor = Mathf.Clamp01((info.Temperature - ignition) / Mathf.Max(ignition, 1e-3f));
            float distortionTempFactor = Mathf.Clamp01(
                (info.Temperature - 293.15f) / Mathf.Max(ignition - 293.15f, 1f));

            GasVisualProfileBuilder.GpuSet profiles = registry != null
                ? GasVisualProfileBuilder.Build(registry)
                : GasVisualProfileBuilder.CoreDefaults;

            float gasFogDensity = PressureFogDensity(info.Pressure, info.Temperature)
                * CompositionVisualDrive(o2Frac, n2Frac, co2Frac, plasmaFrac, profiles);

            float co2ScatterStrength = profiles.Scatter[AtmosConstants.CarbonDioxide.Value].w;
            float plasmaEmissionIntensity = profiles.Emission[AtmosConstants.Plasma.Value].w;

            float fire = info.BurnIntensity;
            float co2SmokeFloor = co2Frac * co2ScatterStrength * PlumeMask(0f) * CoreMask(fire);
            float co2SmokeMid = co2Frac * co2ScatterStrength * PlumeMask(0.5f) * CoreMask(fire);

            float temperatureGradient = EstimateTemperatureGradient(info, simulation);
            float distortionWeight = (gasFogDensity + distortionTempFactor * HeatDistortionBlend)
                * (distortionTempFactor + fire) * (1f + fire * FireDistortionBoost);
            float plasmaGlow = fire > 0.001f || plasmaFrac > 0.002f
                ? plasmaFrac * plasmaEmissionIntensity * glowTempFactor
                : 0f;

            return new AtmosCellVisualMetrics
            {
                TotalMoles = total,
                OxygenFraction = o2Frac,
                NitrogenFraction = n2Frac,
                CarbonDioxideFraction = co2Frac,
                PlasmaFraction = plasmaFrac,
                BurnIntensity = fire,
                GlowTempFactor = glowTempFactor,
                DistortionTempFactor = distortionTempFactor,
                GasFogDensity = gasFogDensity,
                Co2SmokeDriveFloor = co2SmokeFloor,
                Co2SmokeDriveMid = co2SmokeMid,
                TemperatureGradient = temperatureGradient,
                DistortionWeightEstimate = distortionWeight,
                PlasmaGlowEstimate = plasmaGlow,
            };
        }

        public static string FormatCellReport(AtmosCellDebugInfo info, AtmosCellVisualMetrics metrics)
        {
            return
                $"Tile {info.Coord.Grid.x}, {info.Coord.Grid.y} — GPU visual inputs\n" +
                FormatGpuSection(info, metrics);
        }

        public static string FormatGpuSection(AtmosCellDebugInfo info, AtmosCellVisualMetrics metrics)
        {
            return
                $"  Composition: O₂ {metrics.OxygenFraction:P1}  N₂ {metrics.NitrogenFraction:P1}  " +
                $"CO₂ {metrics.CarbonDioxideFraction:P1}  plasma {metrics.PlasmaFraction:P2}\n" +
                $"  gas fog density     {metrics.GasFogDensity:F2}  (P/T ratio {NumberDensityRatio(info.Pressure, info.Temperature):F2}, was {info.Pressure:F0} kPa @ {info.Temperature:F0} K)\n" +
                $"  CO₂ smoke floor     {metrics.Co2SmokeDriveFloor:F2}  mid-plume {metrics.Co2SmokeDriveMid:F2}\n" +
                $"  fire intensity      {metrics.BurnIntensity:F2}  (GPU decays ×{VisualFireDecayPerTick:F2}/tick)\n" +
                $"  glow temp factor    {metrics.GlowTempFactor:F2}  plasma glow est {metrics.PlasmaGlowEstimate:F3}\n" +
                $"  distortion temp     {metrics.DistortionTempFactor:F2}  grad {metrics.TemperatureGradient:F0} K/tile\n" +
                $"  distortion weight   {metrics.DistortionWeightEstimate:F2}\n" +
                $"  Calibration: fire {StrongFireIntensity:F1} @ ~{StrongFireBurnRateMolPerSec:F0} mol/s | " +
                $"gas fog full ≥ {GasFogSaturationPressureKpa:F0} kPa @ 293 K | CO₂ mid ~1.8 drive";
        }

        public static void LogChamberSummary(
            AtmosSimulation simulation,
            GasRegistry registry,
            TileCoord center,
            int radiusX,
            int radiusZ)
        {
            int count = 0;
            float pressureSum = 0f;
            float tempSum = 0f;
            float fireMax = 0f;
            float gasFogMax = 0f;
            float co2MidMax = 0f;
            float distortionWeightMax = 0f;
            float plasmaGlowMax = 0f;

            for (int dz = -radiusZ; dz <= radiusZ; dz++)
            {
                for (int dx = -radiusX; dx <= radiusX; dx++)
                {
                    var coord = new TileCoord(center.MapId, center.Grid.x + dx, center.Grid.y + dz);
                    if (!simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info))
                        continue;

                    AtmosCellVisualMetrics metrics = Compute(info, simulation, registry);
                    count++;
                    pressureSum += info.Pressure;
                    tempSum += info.Temperature;
                    fireMax = Mathf.Max(fireMax, metrics.BurnIntensity);
                    gasFogMax = Mathf.Max(gasFogMax, metrics.GasFogDensity);
                    co2MidMax = Mathf.Max(co2MidMax, metrics.Co2SmokeDriveMid);
                    distortionWeightMax = Mathf.Max(distortionWeightMax, metrics.DistortionWeightEstimate);
                    plasmaGlowMax = Mathf.Max(plasmaGlowMax, metrics.PlasmaGlowEstimate);
                }
            }

            if (count == 0)
            {
                Debug.Log("[AtmosVisual] No simulated cells in chamber region.");
                return;
            }

            Debug.Log(
                $"[AtmosVisual] Chamber {count} cells around {center.Grid.x},{center.Grid.y}\n" +
                $"  avg pressure {pressureSum / count:F1} kPa  avg temp {tempSum / count:F0} K\n" +
                $"  max fire {fireMax:F2}  max gas fog {gasFogMax:F2}  max CO₂ mid-smoke {co2MidMax:F2}\n" +
                $"  max distortion weight {distortionWeightMax:F2}  max plasma glow {plasmaGlowMax:F3}\n" +
                $"  refs: fire {StrongFireIntensity:F1} @ ~{StrongFireBurnRateMolPerSec:F0} mol/s | gas fog full ≥ {GasFogSaturationPressureKpa:F0} kPa");
        }

        static float NumberDensityRatio(float pressureKpa, float temperatureK)
        {
            float t = Mathf.Max(temperatureK, ReferenceTemperatureK);
            float reference = ReferencePressureKpa / ReferenceTemperatureK;
            return (pressureKpa / t) / Mathf.Max(reference, 1e-3f);
        }

        static float PressureFogDensity(float pressureKpa, float temperatureK)
        {
            float excess = Mathf.Max(0f, NumberDensityRatio(pressureKpa, temperatureK) - 1f);
            float scale = FogPressureScale / ReferencePressureKpa;
            return Mathf.Clamp01(excess / Mathf.Max(scale, 1e-3f));
        }

        static float CompositionVisualDrive(
            float o2Frac,
            float n2Frac,
            float co2Frac,
            float plasmaFrac,
            GasVisualProfileBuilder.GpuSet profiles)
        {
            float drive = 0f;
            float[] fractions = { o2Frac, n2Frac, co2Frac, plasmaFrac };
            for (int i = 0; i < fractions.Length; i++)
            {
                if (fractions[i] <= 0.001f)
                    continue;

                float scatter = profiles.Scatter[i].w;
                float emission = profiles.Emission[i].w;
                float distortion = profiles.Misc[i].x;
                drive = Mathf.Max(drive, fractions[i] * Mathf.Max(scatter, Mathf.Max(emission, distortion)));
            }

            return drive;
        }

        static float SafeFraction(float moles, float total)
        {
            return total > 1e-6f ? moles / total : 0f;
        }

        static float PlumeMask(float heightNorm)
        {
            float rise = Mathf.SmoothStep(0.12f, 0.38f, heightNorm);
            float fall = 1f - Mathf.SmoothStep(0.82f, 1f, heightNorm);
            return rise * fall;
        }

        static float CoreMask(float fire)
        {
            return 1f - Mathf.Clamp01(fire * 2.5f);
        }

        static float EstimateTemperatureGradient(AtmosCellDebugInfo info, AtmosSimulation simulation)
        {
            float sample(int dx, int dz)
            {
                var neighbour = new TileCoord(info.Coord.MapId, info.Coord.Grid.x + dx, info.Coord.Grid.y + dz);
                return simulation.TryGetCellDebugInfo(neighbour, out AtmosCellDebugInfo neighbourInfo)
                    ? neighbourInfo.Temperature
                    : info.Temperature;
            }

            float txp = sample(1, 0);
            float txm = sample(-1, 0);
            float tzp = sample(0, 1);
            float tzm = sample(0, -1);
            float gradX = (txp - txm) * 0.5f;
            float gradZ = (tzp - tzm) * 0.5f;
            return Mathf.Sqrt(gradX * gradX + gradZ * gradZ);
        }
    }
}
