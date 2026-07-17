using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Fullscreen pass: composite the vision mask onto the camera color target. Pixels outside the
    /// mask are fully opaque black - a hard cutoff, not a soft fog, so nothing about an unseen tile
    /// leaks through.
    /// </summary>
    public sealed class VisionBlurRenderPass : ScriptableRenderPass
    {
        static readonly MaterialPropertyBlock s_PropertyBlock = new();
        static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int s_FovTexId = Shader.PropertyToID("_FovTex");

        readonly Material _blurMaterial;
        VisionRendererFeature _feature;

        public VisionBlurRenderPass(Material blurMaterial)
        {
            _blurMaterial = blurMaterial;
            renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing + 1);
            profilingSampler = new ProfilingSampler("Vision Blur");
        }

        public void Setup(VisionRendererFeature feature)
        {
            _feature = feature;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_blurMaterial == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (!resourceData.activeColorTexture.IsValid() || !_feature.MaskTexture.IsValid())
                return;

            TextureDesc tempDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            tempDesc.name = "VisionBlurTemp";
            tempDesc.depthBufferBits = 0;
            tempDesc.msaaSamples = MSAASamples.None;
            TextureHandle tempColor = renderGraph.CreateTexture(tempDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out PassData passData, profilingSampler))
            {
                passData.Material = _blurMaterial;
                passData.Source = resourceData.activeColorTexture;
                passData.Mask = _feature.MaskTexture;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.UseTexture(passData.Mask, AccessFlags.Read);
                builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    s_PropertyBlock.Clear();
                    s_PropertyBlock.SetTexture(s_MainTexId, data.Source);
                    s_PropertyBlock.SetTexture(s_FovTexId, data.Mask);

                    context.cmd.DrawProcedural(
                        Matrix4x4.identity,
                        data.Material,
                        0,
                        MeshTopology.Triangles,
                        3,
                        1,
                        s_PropertyBlock);
                });
            }

            RenderGraphUtils.BlitMaterialParameters copyParams = new(
                tempColor,
                resourceData.activeColorTexture,
                Blitter.GetBlitMaterial(TextureDimension.Tex2D),
                0);
            renderGraph.AddBlitPass(copyParams, passName: "Vision Blur Copy");
        }

        sealed class PassData
        {
            public Material Material;
            public TextureHandle Source;
            public TextureHandle Mask;
        }
    }
}
