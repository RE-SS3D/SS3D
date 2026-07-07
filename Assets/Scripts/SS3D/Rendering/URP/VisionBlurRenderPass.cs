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
        static readonly int s_InvViewProjId = Shader.PropertyToID("_PlayerCameraInvViewProj");
        static readonly int s_FogStrengthId = Shader.PropertyToID("_VisionFogStrength");

        readonly Material _blurMaterial;
        VisionRendererFeature _feature;
        float _blurQuality;
        float _blurDirections;
        Vector2 _blurSize;
        float _fogStrength;

        public VisionBlurRenderPass(Material blurMaterial)
        {
            _blurMaterial = blurMaterial;
            renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing + 1);
            profilingSampler = new ProfilingSampler("Vision Blur");
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(VisionRendererFeature feature, float blurQuality, float blurDirections, Vector2 blurSize, float fogStrength)
        {
            _feature = feature;
            _blurQuality = blurQuality;
            _blurDirections = blurDirections;
            _blurSize = blurSize;
            _fogStrength = fogStrength;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_blurMaterial == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.activeColorTexture.IsValid() || !_feature.MaskTexture.IsValid())
                return;

            Camera camera = cameraData.camera;
            Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 viewProjection = gpuProjection * camera.worldToCameraMatrix;
            Matrix4x4 inverseViewProjection = viewProjection.inverse;

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
                passData.BlurQuality = _blurQuality;
                passData.BlurDirections = _blurDirections;
                passData.BlurSize = _blurSize;
                passData.FogStrength = _fogStrength;
                passData.InverseViewProjection = inverseViewProjection;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.UseTexture(passData.Mask, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.Material.SetFloat("_FovBlurQuality", data.BlurQuality);
                    data.Material.SetFloat("_FovBlurDirections", data.BlurDirections);
                    data.Material.SetVector("_FovBlurSize", data.BlurSize);
                    data.Material.SetFloat(s_FogStrengthId, data.FogStrength);
                    data.Material.SetMatrix(s_InvViewProjId, data.InverseViewProjection);

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
            public float BlurQuality;
            public float BlurDirections;
            public Vector2 BlurSize;
            public float FogStrength;
            public Matrix4x4 InverseViewProjection;
        }
    }
}
