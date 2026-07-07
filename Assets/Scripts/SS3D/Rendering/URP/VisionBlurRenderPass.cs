using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Fullscreen pass: blur vision mask and darken hidden pixels on the camera color target.
    /// </summary>
    public sealed class VisionBlurRenderPass : ScriptableRenderPass
    {
        static readonly MaterialPropertyBlock s_PropertyBlock = new();
        static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int s_FovTexId = Shader.PropertyToID("_FovTex");

        readonly Material _blurMaterial;
        VisionRendererFeature _feature;
        float _blurQuality;
        float _blurDirections;
        Vector2 _blurSize;

        public VisionBlurRenderPass(Material blurMaterial)
        {
            _blurMaterial = blurMaterial;
            renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing + 1);
            profilingSampler = new ProfilingSampler("Vision Blur");
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(VisionRendererFeature feature, float blurQuality, float blurDirections, Vector2 blurSize)
        {
            _feature = feature;
            _blurQuality = blurQuality;
            _blurDirections = blurDirections;
            _blurSize = blurSize;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_blurMaterial == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.activeColorTexture.IsValid() || !_feature.MaskTexture.IsValid())
                return;

            Matrix4x4 view = cameraData.camera.worldToCameraMatrix;
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, false);
            Matrix4x4 inverseViewProjection = (projection * view).inverse;

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
                passData.InverseViewProjection = inverseViewProjection;
                passData.BlurQuality = _blurQuality;
                passData.BlurDirections = _blurDirections;
                passData.BlurSize = _blurSize;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.UseTexture(passData.Mask, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.Material.SetMatrix("_PlayerCameraInvViewProj", data.InverseViewProjection);
                    data.Material.SetFloat("_FovBlurQuality", data.BlurQuality);
                    data.Material.SetFloat("_FovBlurDirections", data.BlurDirections);
                    data.Material.SetVector("_FovBlurSize", data.BlurSize);

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
            public Matrix4x4 InverseViewProjection;
            public float BlurQuality;
            public float BlurDirections;
            public Vector2 BlurSize;
        }
    }
}
