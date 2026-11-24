using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Publishes the <see cref="StationShading"/> volume values to the station shaders
    /// (<c>StationFlatLighting.hlsl</c>) before each camera renders. It adds no passes, so it stays independent
    /// of <see cref="CortaoAmbientOcclusionFeature"/>: turning AO off no longer leaves the station on the
    /// shader's fallback defaults. With this feature off or missing, the shaders keep the last published values,
    /// or the <see cref="StationShading"/> defaults when nothing was published.
    /// </summary>
    public sealed class StationShadingFeature : ScriptableRendererFeature
    {
        private static readonly int StationAmbientId = Shader.PropertyToID("_SS3DStationAmbient");
        private static readonly int StationLightsId = Shader.PropertyToID("_SS3DStationLights");
        private static readonly int StationFalloffId = Shader.PropertyToID("_SS3DStationFalloff");

        public override void Create()
        {
        }

        // Runs after URP has updated the volume stack for this camera, so each camera publishes its own values.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Publish(VolumeManager.instance.stack?.GetComponent<StationShading>());
        }

        /// <summary>Sets the station globals from <paramref name="shading"/>, or its defaults when null.</summary>
        public static void Publish(StationShading shading)
        {
            Shader.SetGlobalVector(
                StationAmbientId,
                shading != null ? shading.ToAmbientParams() : StationShading.DefaultAmbientParams);
            Shader.SetGlobalVector(
                StationLightsId,
                shading != null ? shading.ToLightParams() : StationShading.DefaultLightParams);
            Shader.SetGlobalVector(
                StationFalloffId,
                shading != null ? shading.ToFalloffParams() : StationShading.DefaultFalloffParams);
        }
    }
}
