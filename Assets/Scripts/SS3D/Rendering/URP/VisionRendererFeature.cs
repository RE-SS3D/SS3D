using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// URP fog-of-war fullscreen mask + blur composite for the player camera.
    /// </summary>
    public sealed class VisionRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _visionMaskShader;
        [SerializeField] private Shader _visionBlurShader;
        [SerializeField] private float _blurQuality = 5f;
        [SerializeField] private float _blurDirections = 25f;
        [SerializeField] private Vector2 _blurSize = new(5f, 5f);
        [SerializeField] [Tooltip("Output the raw visibility mask to the screen and skip the blur composite (diagnostic).")] private bool _debugMask;

        VisionMaskRenderPass _maskPass;
        VisionBlurRenderPass _blurPass;
        Material _maskMaterial;
        Material _blurMaterial;

        internal TextureHandle MaskTexture { get; set; }

        public override void Create()
        {
            _visionMaskShader ??= Shader.Find("Vision/VisionMask");
            _visionBlurShader ??= Shader.Find("Vision/VisionMaskBlur");

            if (_visionMaskShader != null && _maskMaterial == null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(_visionMaskShader);

            if (_visionBlurShader != null && _blurMaterial == null)
                _blurMaterial = CoreUtils.CreateEngineMaterial(_visionBlurShader);

            _maskPass = new VisionMaskRenderPass(_maskMaterial);
            _blurPass = new VisionBlurRenderPass(_blurMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_maskMaterial == null || _blurMaterial == null || !VisionRenderContext.Enabled)
                return;

            Camera camera = renderingData.cameraData.camera;
            if (camera == null || camera.cameraType != CameraType.Game)
                return;

            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            _maskPass.Setup(this, _debugMask);
            renderer.EnqueuePass(_maskPass);

            if (_debugMask)
                return;

            _blurPass.Setup(this, _blurQuality, _blurDirections, _blurSize);
            renderer.EnqueuePass(_blurPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_maskMaterial);
            CoreUtils.Destroy(_blurMaterial);
            _maskMaterial = null;
            _blurMaterial = null;
        }
    }
}
