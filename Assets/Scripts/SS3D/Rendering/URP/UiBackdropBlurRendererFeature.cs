using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Fullscreen Dual Kawase blur gated by <see cref="UiBackdropBlurContext"/>.
    /// Registered on the Forward+ renderer; strength comes from screen-effects / machine UI.
    /// </summary>
    public sealed class UiBackdropBlurRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader _blurShader;

        UiBackdropBlurRenderPass _pass;
        Material _material;

        public override void Create()
        {
            _blurShader ??= Shader.Find("SS3D/UI/UiBackdropBlur");

            if (_blurShader != null && _material == null)
            {
                _material = CoreUtils.CreateEngineMaterial(_blurShader);
            }

            _pass = new UiBackdropBlurRenderPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null || UiBackdropBlurContext.Intensity < 0.001f)
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            if (camera == null || camera.cameraType != CameraType.Game)
            {
                return;
            }

            if (camera.targetTexture != null)
            {
                return;
            }

            if (renderingData.cameraData.renderType != CameraRenderType.Base)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }
    }
}
