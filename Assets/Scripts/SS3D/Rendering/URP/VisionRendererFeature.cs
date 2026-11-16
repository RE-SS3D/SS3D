using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// URP fog-of-war for the player camera (fov.md §3): a visibility mask from the polar raycast map and
    /// seen-wall tiles produced by <c>VisionSubSystem</c>, softened inward and composited. Unseen areas are
    /// fully opaque black - a mask, not a soft fog.
    /// </summary>
    public sealed class VisionRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _visionMaskShader;
        [SerializeField] private Shader _visionCompositeShader;
        [SerializeField] [Tooltip("Output the raw visibility mask to the screen and skip the composite (diagnostic).")] private bool _debugMask;

        VisionMaskRenderPass _maskPass;
        VisionCompositeRenderPass _compositePass;
        Material _maskMaterial;
        Material _compositeMaterial;

        internal TextureHandle MaskTexture { get; set; }

        public override void Create()
        {
            _visionMaskShader ??= Shader.Find("Vision/VisionMask");
            _visionCompositeShader ??= Shader.Find("Vision/VisionComposite");

            if (_visionMaskShader != null && _maskMaterial == null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(_visionMaskShader);

            if (_visionCompositeShader != null && _compositeMaterial == null)
                _compositeMaterial = CoreUtils.CreateEngineMaterial(_visionCompositeShader);

            _maskPass = new VisionMaskRenderPass(_maskMaterial);
            _compositePass = new VisionCompositeRenderPass(_compositeMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_maskMaterial == null || _compositeMaterial == null || !VisionRenderContext.Enabled)
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

            _compositePass.Setup(this);
            renderer.EnqueuePass(_compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_maskMaterial);
            CoreUtils.Destroy(_compositeMaterial);
            _maskMaterial = null;
            _compositeMaterial = null;
        }
    }
}
