using System.Collections.Generic;
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
    /// <para>
    /// A camera registered with <see cref="SetUnlit"/> (the character preview booth) gets flat albedo instead, whatever
    /// its volumes say, and the volume values are put back as soon as it has rendered, so nothing else is drawn flat.
    /// Not a Volume: a global volume on a layer reaches every camera whose volume mask includes that layer, and the
    /// player camera's mask is Everything.
    /// </para>
    /// </summary>
    public sealed class StationShadingFeature : ScriptableRendererFeature
    {
        private static readonly int StationAmbientId = Shader.PropertyToID("_SS3DStationAmbient");
        private static readonly int StationLightsId = Shader.PropertyToID("_SS3DStationLights");
        private static readonly int StationFalloffId = Shader.PropertyToID("_SS3DStationFalloff");

        /// <summary>albedo × (ambient × AO + lights): ambient 1 without occlusion, no fixture light.</summary>
        private static readonly Vector4 UnlitAmbient = new(1f, 0f, 0f, 1f);
        private static readonly Vector4 UnlitLights = new(0f, StationShading.DefaultLightWrap, StationShading.DefaultLightRolloff,
            StationShading.DefaultLightContrast);

        private static readonly HashSet<Camera> UnlitCameras = new();
        private static Vector4 _ambient = StationShading.DefaultAmbientParams;
        private static Vector4 _lights = StationShading.DefaultLightParams;
        private static Vector4 _falloff = StationShading.DefaultFalloffParams;
        private static bool _restoreHooked;

        /// <summary>Draws the station shaders flat (albedo only) for <paramref name="camera"/> alone, or stops.</summary>
        public static void SetUnlit(Camera camera, bool unlit)
        {
            if (camera == null)
            {
                return;
            }

            if (!unlit)
            {
                UnlitCameras.Remove(camera);
                return;
            }

            UnlitCameras.Add(camera);
            if (!_restoreHooked)
            {
                _restoreHooked = true;
                RenderPipelineManager.endCameraRendering += RestoreAfterUnlit;
            }
        }

        /// <summary>The unlit camera has submitted: later cameras without this feature see the volume values again.</summary>
        private static void RestoreAfterUnlit(ScriptableRenderContext context, Camera camera)
        {
            if (!UnlitCameras.Contains(camera))
            {
                return;
            }

            SetGlobals(_ambient, _lights, _falloff);
        }

        public override void Create()
        {
        }

        // Runs after URP has updated the volume stack for this camera, so each camera publishes its own values.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (UnlitCameras.Contains(renderingData.cameraData.camera))
            {
                SetGlobals(UnlitAmbient, UnlitLights, _falloff);
                return;
            }

            Publish(VolumeManager.instance.stack?.GetComponent<StationShading>());
        }

        /// <summary>Sets the station globals from <paramref name="shading"/>, or its defaults when null.</summary>
        public static void Publish(StationShading shading)
        {
            _ambient = shading != null ? shading.ToAmbientParams() : StationShading.DefaultAmbientParams;
            _lights = shading != null ? shading.ToLightParams() : StationShading.DefaultLightParams;
            _falloff = shading != null ? shading.ToFalloffParams() : StationShading.DefaultFalloffParams;
            SetGlobals(_ambient, _lights, _falloff);
        }

        private static void SetGlobals(Vector4 ambient, Vector4 lights, Vector4 falloff)
        {
            Shader.SetGlobalVector(StationAmbientId, ambient);
            Shader.SetGlobalVector(StationLightsId, lights);
            Shader.SetGlobalVector(StationFalloffId, falloff);
        }
    }
}
