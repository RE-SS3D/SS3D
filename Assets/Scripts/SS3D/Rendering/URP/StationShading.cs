using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Volume override for the station's flat-with-lighting shading (STDefault / STTransparent, see
    /// <c>StationFlatLighting.hlsl</c>): <c>albedo × (ambient × AO + lights) + emission</c>. Published to shaders
    /// each camera by <see cref="CortaoAmbientOcclusionFeature"/>; the shader falls back to these same defaults when
    /// nothing is published.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("SS3D/Station Shading")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StationShading : VolumeComponent
    {
        // Old-SS3D look, "unlit + lighting": no ambient; range-relative fixture light, pushed onto the rolloff
        // plateau so lit surfaces read flat at their albedo, with a steep contrast edge into the dark gaps.
        // Keep in sync with Assets/DefaultVolumeProfile.asset and the fallback in StationFlatLighting.hlsl.
        public const float DefaultAmbient = 0f;
        public const float DefaultAmbientOcclusion = 1f;
        public const float DefaultLightStrength = 4f;
        public const float DefaultLightWrap = 1f;
        public const float DefaultLightRolloff = 1f;
        public const float DefaultLightOcclusion = 0.4f;
        public const float DefaultLightContrast = 2.5f;
        public const float DefaultLightFalloff = 1f;

        [Tooltip("Flat, direction-free albedo level every surface gets. 1 = the authored colour.")]
        public ClampedFloatParameter Ambient = new(DefaultAmbient, 0f, 2f);

        [Tooltip("How strongly CORTAO occlusion darkens the ambient level.")]
        public ClampedFloatParameter AmbientOcclusion = new(DefaultAmbientOcclusion, 0f, 1f);

        [Tooltip("Multiplier on fixture light (all URP lights, with their shadows) added on top of the ambient. With "
            + "rolloff 1, higher values push more of each pool onto the flat plateau, so pools grow rather than glow.")]
        public ClampedFloatParameter LightStrength = new(DefaultLightStrength, 0f, 8f);

        [Tooltip("How far light wraps past the terminator. 0 = Lambert, 1 = faces at 90° still get half. Back "
            + "faces stay unlit so unshadowed fill lights do not shine through walls.")]
        public ClampedFloatParameter LightWrap = new(DefaultLightWrap, 0f, 1f);

        [Tooltip("Plateau for fixture light: light / (1 + rolloff × light), which levels off at 1 / rolloff. 1 = fully "
            + "lit surfaces flatten to their albedo (the unlit look); below 1 they keep brightening toward hot spots; "
            + "0 = linear.")]
        public ClampedFloatParameter LightRolloff = new(DefaultLightRolloff, 0f, 2f);

        [Tooltip("How strongly CORTAO occlusion darkens fixture light.")]
        public ClampedFloatParameter LightOcclusion = new(DefaultLightOcclusion, 0f, 1f);

        [Tooltip("Shadow depth: exponent on fixture light before rolloff. 1 = linear; above 1 darkens dim and "
            + "shadowed light (fill spill, light leaking into shadows, falloff between fixtures) much more than "
            + "bright pools.")]
        public ClampedFloatParameter LightContrast = new(DefaultLightContrast, 1f, 3f);

        [Tooltip("Distance falloff of fixture light. 0 = URP's physical 1/d² (hot spot at the fixture, gone within a "
            + "few metres). 1 = range-relative like Unity's Built-in pipeline and old SS3D: light keeps its strength "
            + "across most of its range, filling rooms with broad pools and leaving darker gaps between fixtures.")]
        public ClampedFloatParameter LightFalloff = new(DefaultLightFalloff, 0f, 1f);

        /// <summary>Packs <c>_SS3DStationAmbient</c>; w = 1 tells the shader the values were published.</summary>
        public Vector4 ToAmbientParams()
        {
            return new Vector4(Ambient.value, AmbientOcclusion.value, LightOcclusion.value, 1f);
        }

        /// <summary>Packs <c>_SS3DStationLights</c>.</summary>
        public Vector4 ToLightParams()
        {
            return new Vector4(LightStrength.value, LightWrap.value, LightRolloff.value, LightContrast.value);
        }

        /// <summary>Packs <c>_SS3DStationFalloff</c>: x = falloff blend.</summary>
        public Vector4 ToFalloffParams()
        {
            return new Vector4(LightFalloff.value, 0f, 0f, 0f);
        }

        public static Vector4 DefaultFalloffParams => new(DefaultLightFalloff, 0f, 0f, 0f);

        public static Vector4 DefaultAmbientParams =>
            new(DefaultAmbient, DefaultAmbientOcclusion, DefaultLightOcclusion, 1f);

        public static Vector4 DefaultLightParams =>
            new(DefaultLightStrength, DefaultLightWrap, DefaultLightRolloff, DefaultLightContrast);

        /// <summary>True when either term reads the occlusion texture, so the AO passes must run.</summary>
        public bool UsesOcclusion()
        {
            bool ambientUsesAo = Ambient.value > 0f && AmbientOcclusion.value > 0f;
            bool lightUsesAo = LightStrength.value > 0f && LightOcclusion.value > 0f;
            return ambientUsesAo || lightUsesAo;
        }
    }
}
