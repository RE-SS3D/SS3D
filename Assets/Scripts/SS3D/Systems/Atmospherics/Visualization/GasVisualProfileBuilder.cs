using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Packs <see cref="GasVisualProfile"/> data for the composition atlas channels uploaded to GPU.
    /// </summary>
    public static class GasVisualProfileBuilder
    {
        public const int CompositionChannels = 4;

        public struct GpuSet
        {
            public Vector4[] Scatter;
            public Vector4[] Emission;
            public Vector4[] Misc;
        }

        static readonly GpuSet s_DefaultSet = BuildCoreDefaults();
        static GasRegistry s_CachedRegistry;
        static GpuSet s_CachedSet;

        public static GpuSet CoreDefaults => s_DefaultSet;

        /// <summary>
        /// Packs visual profiles for GPU upload. Results are cached per registry instance so
        /// the atmos tick path does not allocate every frame.
        /// </summary>
        public static GpuSet Build(GasRegistry registry)
        {
            if (registry == null)
                return s_DefaultSet;

            if (ReferenceEquals(registry, s_CachedRegistry) && s_CachedSet.Scatter != null)
                return s_CachedSet;

            var scatter = new Vector4[CompositionChannels];
            var emission = new Vector4[CompositionChannels];
            var misc = new Vector4[CompositionChannels];

            for (int gasId = 0; gasId < CompositionChannels; gasId++)
            {
                GasVisualProfile profile = ResolveProfile(registry, (ushort)gasId);
                PackProfile(profile, scatter, emission, misc, gasId);
            }

            s_CachedSet = new GpuSet { Scatter = scatter, Emission = emission, Misc = misc };
            s_CachedRegistry = registry;
            return s_CachedSet;
        }

        static GpuSet BuildCoreDefaults()
        {
            var scatter = new Vector4[CompositionChannels];
            var emission = new Vector4[CompositionChannels];
            var misc = new Vector4[CompositionChannels];

            for (int gasId = 0; gasId < CompositionChannels; gasId++)
            {
                PackProfile(CoreDefaultProfile((ushort)gasId), scatter, emission, misc, gasId);
            }

            return new GpuSet { Scatter = scatter, Emission = emission, Misc = misc };
        }

        static GasVisualProfile ResolveProfile(GasRegistry registry, ushort gasId)
        {
            if (registry != null && registry.TryGetDefinition(new GasId(gasId), out GasDefinition definition))
                return definition.VisualProfile;

            return CoreDefaultProfile(gasId);
        }

        public static GasVisualProfile CoreDefaultProfile(ushort gasId)
        {
            if (gasId == AtmosConstants.Oxygen.Value)
                return new GasVisualProfile
                {
                    ScatterColor = new Color(0.92f, 0.95f, 1f, 1f),
                    ScatterStrength = 0.35f,
                    EmissionColor = Color.clear,
                    EmissionIntensity = 0f,
                    DistortionScale = 0f,
                    TurbulenceScale = 0f,
                };
            if (gasId == AtmosConstants.Nitrogen.Value)
                return new GasVisualProfile
                {
                    ScatterColor = new Color(0.92f, 0.95f, 1f, 1f),
                    ScatterStrength = 0.35f,
                    EmissionColor = Color.clear,
                    EmissionIntensity = 0f,
                    DistortionScale = 0f,
                    TurbulenceScale = 0f,
                };
            if (gasId == AtmosConstants.CarbonDioxide.Value)
                return new GasVisualProfile
                {
                    ScatterColor = new Color(0.18f, 0.16f, 0.14f, 1f),
                    ScatterStrength = 4f,
                    EmissionColor = Color.clear,
                    EmissionIntensity = 0f,
                    DistortionScale = 0f,
                    TurbulenceScale = 0f,
                };
            if (gasId == AtmosConstants.Plasma.Value)
                return new GasVisualProfile
                {
                    ScatterColor = Color.clear,
                    ScatterStrength = 0f,
                    EmissionColor = new Color(0.75f, 0.2f, 1f, 1f),
                    EmissionIntensity = 1f,
                    DistortionScale = 1f,
                    TurbulenceScale = 1f,
                };

            return GasVisualProfile.Invisible;
        }

        static void PackProfile(
            GasVisualProfile profile,
            Vector4[] scatter,
            Vector4[] emission,
            Vector4[] misc,
            int gasId)
        {
            scatter[gasId] = new Vector4(
                profile.ScatterColor.r,
                profile.ScatterColor.g,
                profile.ScatterColor.b,
                profile.ScatterStrength);

            emission[gasId] = new Vector4(
                profile.EmissionColor.r,
                profile.EmissionColor.g,
                profile.EmissionColor.b,
                profile.EmissionIntensity);

            misc[gasId] = new Vector4(profile.DistortionScale, profile.TurbulenceScale, 0f, 0f);
        }
    }
}
