using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Softens the vision mask inward and composites it (2026-11_fov-rework D7): a separable blur of the
    /// mask at half resolution, then the scene times mask × blur(mask) into a new colour target that
    /// becomes the camera colour. Hidden pixels stay fully black, so nothing about an unseen tile leaks.
    /// </summary>
    public sealed class VisionBlurRenderPass : ScriptableRenderPass
    {
        /// <summary>How far the fade reaches into what is visible, as a fraction of screen height.</summary>
        const float EdgeWidth = 0.005f;

        const int HorizontalPass = 0;
        const int VerticalPass = 1;
        const int CompositePass = 2;

        static readonly MaterialPropertyBlock s_PropertyBlock = new();
        static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int s_MaskTexId = Shader.PropertyToID("_VisionMaskTex");
        static readonly int s_BlurTexId = Shader.PropertyToID("_VisionBlurTex");
        static readonly int s_BlurStepId = Shader.PropertyToID("_VisionBlurStep");

        readonly Material _material;
        VisionRendererFeature _feature;

        public VisionBlurRenderPass(Material material)
        {
            _material = material;
            renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing + 1);
            profilingSampler = new ProfilingSampler("Vision Composite");
        }

        public void Setup(VisionRendererFeature feature)
        {
            _feature = feature;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null || _feature == null || !VisionRenderContext.Enabled)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (!resourceData.activeColorTexture.IsValid() || !_feature.MaskTexture.IsValid() || resourceData.isActiveTargetBackBuffer)
                return;

            TextureDesc colorDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            TextureDesc halfDesc = new(Mathf.Max(1, colorDesc.width / 2), Mathf.Max(1, colorDesc.height / 2))
            {
                format = GraphicsFormat.R8_UNorm,
                msaaSamples = MSAASamples.None,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            halfDesc.name = "VisionBlurH";
            TextureHandle blurH = renderGraph.CreateTexture(halfDesc);
            halfDesc.name = "VisionBlur";
            TextureHandle blur = renderGraph.CreateTexture(halfDesc);

            TextureDesc outputDesc = colorDesc;
            outputDesc.name = "VisionComposite";
            outputDesc.depthBufferBits = DepthBits.None;
            outputDesc.msaaSamples = MSAASamples.None;
            TextureHandle output = renderGraph.CreateTexture(outputDesc);

            // Eight steps span the edge width; the horizontal step is scaled so both run the same screen distance.
            float stepY = EdgeWidth / 8f;
            float stepX = stepY * colorDesc.height / Mathf.Max(1, colorDesc.width);
            Vector4 step = new(stepX, stepY, 0f, 0f);

            TextureHandle mask = _feature.MaskTexture;
            AddPass(renderGraph, "Vision Blur Horizontal", HorizontalPass, blurH, step, TextureHandle.nullHandle, mask, TextureHandle.nullHandle);
            AddPass(renderGraph, "Vision Blur Vertical", VerticalPass, blur, step, TextureHandle.nullHandle, TextureHandle.nullHandle, blurH);
            AddPass(renderGraph, "Vision Composite", CompositePass, output, step, resourceData.activeColorTexture, mask, blur);

            resourceData.cameraColor = output;
        }

        void AddPass(RenderGraph renderGraph, string name, int pass, TextureHandle target, Vector4 step,
            TextureHandle color, TextureHandle mask, TextureHandle blur)
        {
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(name, out PassData passData, profilingSampler);
            passData.Material = _material;
            passData.Pass = pass;
            passData.Step = step;
            passData.Color = color;
            passData.Mask = mask;
            passData.Blur = blur;

            if (color.IsValid())
                builder.UseTexture(color, AccessFlags.Read);
            if (mask.IsValid())
                builder.UseTexture(mask, AccessFlags.Read);
            if (blur.IsValid())
                builder.UseTexture(blur, AccessFlags.Read);

            builder.SetRenderAttachment(target, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                s_PropertyBlock.Clear();
                if (data.Color.IsValid())
                    s_PropertyBlock.SetTexture(s_MainTexId, data.Color);
                if (data.Mask.IsValid())
                    s_PropertyBlock.SetTexture(s_MaskTexId, data.Mask);
                if (data.Blur.IsValid())
                    s_PropertyBlock.SetTexture(s_BlurTexId, data.Blur);
                s_PropertyBlock.SetVector(s_BlurStepId, data.Step);

                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, data.Pass, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
            });
        }

        sealed class PassData
        {
            public Material Material;
            public int Pass;
            public Vector4 Step;
            public TextureHandle Color;
            public TextureHandle Mask;
            public TextureHandle Blur;
        }
    }
}
