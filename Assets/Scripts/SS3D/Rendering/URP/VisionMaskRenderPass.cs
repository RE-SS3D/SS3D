using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Fullscreen pass: scene depth → vision visibility mask render target.
    /// </summary>
    public sealed class VisionMaskRenderPass : ScriptableRenderPass
    {
        static readonly int s_InvViewProjId = Shader.PropertyToID("_PlayerCameraInvViewProj");

        readonly Material _maskMaterial;
        VisionRendererFeature _feature;
        bool _debugToScreen;

        public VisionMaskRenderPass(Material maskMaterial)
        {
            _maskMaterial = maskMaterial;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("Vision Mask");
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(VisionRendererFeature feature, bool debugToScreen)
        {
            _feature = feature;
            _debugToScreen = debugToScreen;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_maskMaterial == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.activeColorTexture.IsValid())
                return;

            Camera camera = cameraData.camera;
            // GPU projection must match the matrix used when the camera depth texture was
            // written (same as Seteron's VisionMaskEffect). Atmos's nonJittered path is for
            // plane unproject, not depth reconstruction.
            Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 viewProjection = gpuProjection * camera.worldToCameraMatrix;
            _maskMaterial.SetMatrix(s_InvViewProjId, viewProjection.inverse);

            TextureDesc maskDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            maskDesc.name = "VisionMask";
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = MSAASamples.None;
            maskDesc.clearBuffer = true;

            TextureHandle maskTexture = renderGraph.CreateTexture(maskDesc);
            _feature.MaskTexture = maskTexture;

            RenderGraphUtils.BlitMaterialParameters blitParams = new(
                resourceData.activeColorTexture,
                maskTexture,
                _maskMaterial,
                0);

            using (var builder = renderGraph.AddBlitPass(blitParams, passName: "Vision Mask", returnBuilder: true))
            {
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
            }

            if (_debugToScreen)
            {
                RenderGraphUtils.BlitMaterialParameters debugParams = new(
                    maskTexture,
                    resourceData.activeColorTexture,
                    Blitter.GetBlitMaterial(TextureDimension.Tex2D),
                    0);
                renderGraph.AddBlitPass(debugParams, passName: "Vision Mask Debug");
            }
        }
    }
}
