using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Dual Kawase pyramid blur of the camera color target. Much stronger than URP Gaussian DoF,
    /// used so the world softens heavily behind diegetic machine UI overlays.
    /// </summary>
    public sealed class UiBackdropBlurRenderPass : ScriptableRenderPass
    {
        static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        static readonly int BlitTexelSizeId = Shader.PropertyToID("_BlitTexture_TexelSize");
        static readonly int BlurOffsetId = Shader.PropertyToID("_BlurOffset");
        static readonly MaterialPropertyBlock s_PropertyBlock = new();

        const int MaxIterations = 4;

        readonly Material _material;

        public UiBackdropBlurRenderPass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("UI Backdrop Blur");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            float intensity = UiBackdropBlurContext.Intensity;
            if (_material == null || intensity < 0.001f)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle activeColor = resourceData.activeColorTexture;
            if (!activeColor.IsValid())
            {
                return;
            }

            // intensity 1 ≈ soft modal focus (3 half-res steps), not a milky wash.
            int iterations = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 3.5f, intensity)), 2, MaxIterations);
            float offset = Mathf.Lerp(0.75f, 1.5f, intensity);

            TextureDesc sourceDesc = activeColor.GetDescriptor(renderGraph);
            TextureHandle[] pyramid = new TextureHandle[iterations];

            TextureHandle current = activeColor;
            for (int i = 0; i < iterations; i++)
            {
                TextureDesc downDesc = sourceDesc;
                downDesc.name = $"UiBackdropBlurDown{i}";
                downDesc.width = Mathf.Max(1, sourceDesc.width >> (i + 1));
                downDesc.height = Mathf.Max(1, sourceDesc.height >> (i + 1));
                downDesc.depthBufferBits = 0;
                downDesc.msaaSamples = MSAASamples.None;
                downDesc.clearBuffer = false;
                pyramid[i] = renderGraph.CreateTexture(downDesc);
                RecordKawaseBlit(renderGraph, current, pyramid[i], passIndex: 0, offset, $"UI Backdrop Down {i}");
                current = pyramid[i];
            }

            for (int i = iterations - 2; i >= 0; i--)
            {
                RecordKawaseBlit(renderGraph, current, pyramid[i], passIndex: 1, offset, $"UI Backdrop Up {i}");
                current = pyramid[i];
            }

            RecordKawaseBlit(renderGraph, current, activeColor, passIndex: 1, offset, "UI Backdrop Final");
        }

        void RecordKawaseBlit(
            RenderGraph renderGraph,
            TextureHandle source,
            TextureHandle destination,
            int passIndex,
            float offset,
            string passName)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(
                passName,
                out PassData passData,
                profilingSampler);

            passData.Material = _material;
            passData.Source = source;
            passData.PassIndex = passIndex;
            passData.Offset = offset;

            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                s_PropertyBlock.Clear();
                RTHandle sourceHandle = data.Source;
                if (sourceHandle != null)
                {
                    s_PropertyBlock.SetTexture(BlitTextureId, sourceHandle);
                    RenderTexture rt = sourceHandle.rt;
                    if (rt != null)
                    {
                        float width = rt.width;
                        float height = rt.height;
                        s_PropertyBlock.SetVector(
                            BlitTexelSizeId,
                            new Vector4(1f / width, 1f / height, width, height));
                    }
                }

                s_PropertyBlock.SetFloat(BlurOffsetId, data.Offset);
                context.cmd.DrawProcedural(
                    Matrix4x4.identity,
                    data.Material,
                    data.PassIndex,
                    MeshTopology.Triangles,
                    3,
                    1,
                    s_PropertyBlock);
            });
        }

        sealed class PassData
        {
            public Material Material;
            public TextureHandle Source;
            public int PassIndex;
            public float Offset;
        }
    }
}
