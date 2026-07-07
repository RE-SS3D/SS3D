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
        readonly Material _maskMaterial;
        VisionRendererFeature _feature;

        public VisionMaskRenderPass(Material maskMaterial)
        {
            _maskMaterial = maskMaterial;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("Vision Mask");
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(VisionRendererFeature feature)
        {
            _feature = feature;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_maskMaterial == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.activeColorTexture.IsValid())
                return;

            TextureDesc maskDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            maskDesc.name = "VisionMask";
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = 1;
            maskDesc.clearBuffer = true;

            TextureHandle maskTexture = renderGraph.CreateTexture(maskDesc);
            _feature.MaskTexture = maskTexture;

            Matrix4x4 view = cameraData.camera.worldToCameraMatrix;
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, false);
            Matrix4x4 inverseViewProjection = (projection * view).inverse;
            _maskMaterial.SetMatrix("_PlayerCameraInvViewProj", inverseViewProjection);

            RenderGraphUtils.BlitMaterialParameters blitParams = new(
                resourceData.activeColorTexture,
                maskTexture,
                _maskMaterial,
                0);

            using (var builder = renderGraph.AddBlitPass(blitParams, passName: "Vision Mask", returnBuilder: true))
            {
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
            }
        }
    }
}
