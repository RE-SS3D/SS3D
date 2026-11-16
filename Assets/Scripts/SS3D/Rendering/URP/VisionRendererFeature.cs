using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// URP fog-of-war fullscreen mask + hard composite for the player camera. Visibility comes
    /// from the polar raycast map produced by <c>VisionSubSystem</c>; unseen areas are fully
    /// opaque black - a mask, not a soft fog.
    /// </summary>
    public sealed class VisionRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _visionMaskShader;
        [SerializeField] private Shader _visionBlurShader;
        [SerializeField] [Tooltip("Output the raw visibility mask to the screen and skip the composite (diagnostic).")] private bool _debugMask;

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
            if (_maskMaterial == null || _blurMaterial == null || !VisionRenderContext.Enabled ||
                VisionRenderContext.Suppressed)
                return;

            Camera camera = renderingData.cameraData.camera;
            if (camera == null || camera.cameraType != CameraType.Game)
                return;

            // Inventory icons use RuntimePreviewGenerator (CameraType.Game + targetTexture).
            // FOV globals are world-space for the player camera, so applying the mask here
            // paints every preview pixel black.
            if (camera.targetTexture != null)
                return;

            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            _maskPass.Setup(this, _debugMask);
            renderer.EnqueuePass(_maskPass);

            if (_debugMask)
                return;

            _blurPass.Setup(this);
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
