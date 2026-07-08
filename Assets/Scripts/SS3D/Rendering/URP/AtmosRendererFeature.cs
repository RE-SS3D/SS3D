using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// URP feature that composites turf gas scattering over the camera color buffer.
    /// </summary>
    public sealed class AtmosRendererFeature : ScriptableRendererFeature
    {
        public enum DebugView
        {
            Off = 0,
            Mask = 1,
            Pressure = 2,
            Temperature = 3,
            Fire = 4,
            Composition = 5,
            AtlasUV = 6,
        }

        private static readonly int AtmosPressureId = Shader.PropertyToID("_AtmosPressure");
        private static readonly int AtmosTemperatureId = Shader.PropertyToID("_AtmosTemperature");
        private static readonly int AtmosCompositionId = Shader.PropertyToID("_AtmosComposition");
        private static readonly int AtmosFireId = Shader.PropertyToID("_AtmosFire");
        private static readonly int AtmosMaskId = Shader.PropertyToID("_AtmosMask");
        private static readonly int AtmosAtlasBoundsId = Shader.PropertyToID("_AtmosAtlasBounds");
        private static readonly int AtmosVolumeHeightId = Shader.PropertyToID("_AtmosVolumeHeight");
        private static readonly int AtmosReferencePressureId = Shader.PropertyToID("_AtmosReferencePressure");
        private static readonly int AtmosFogPressureScaleId = Shader.PropertyToID("_AtmosFogPressureScale");
        private static readonly int AtmosScatterStrengthId = Shader.PropertyToID("_AtmosScatterStrength");
        private static readonly int AtmosScatterColorId = Shader.PropertyToID("_AtmosScatterColor");
        private static readonly int AtmosSlabStepsId = Shader.PropertyToID("_AtmosSlabSteps");
        private static readonly int AtmosDebugViewId = Shader.PropertyToID("_AtmosDebugView");
        private static readonly int AtmosInvViewProjId = Shader.PropertyToID("_AtmosInvViewProj");

        [SerializeField] private Shader _scatterShader;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private DebugView _debugView = DebugView.Off;
        [SerializeField] private float _scatterStrength = 1f;
        [SerializeField] private Color _scatterColor = new(0.65f, 0.7f, 0.75f, 1f);
        [SerializeField] private float _volumeHeight = 2.5f;
        [SerializeField] private float _referencePressure = 101.325f;
        [SerializeField] private float _fogPressureScale = 80f;
        [SerializeField] [Range(1, 8)] private int _slabSteps = 6;

        AtmosScatterPass _scatterPass;
        Material _scatterMaterial;

        public override void Create()
        {
            if (_scatterShader == null)
                _scatterShader = Shader.Find("Custom/AtmosScatter");

            if (_scatterShader != null && _scatterMaterial == null)
                _scatterMaterial = CoreUtils.CreateEngineMaterial(_scatterShader);

            _scatterPass = new AtmosScatterPass(_scatterMaterial)
            {
                // Composite after post-processing so the debug overlay is stable while still
                // running on an intermediate target that can be sampled safely.
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
                // Required so activeColorTexture can be sampled as _BlitTexture (not the back buffer).
                requiresIntermediateTexture = true,
            };
            _scatterPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_enabled || _scatterMaterial == null)
                return;

            if (!AtmosRenderContext.TryGetRequest(out AtmosRenderContext.Request request))
                return;

            if (renderingData.cameraData.camera != request.SourceCamera)
                return;

            if (!AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot))
                return;

            if (snapshot.Pressure == null || snapshot.Mask == null)
                return;

            // Debug views still render when scatter strength is zero so the atlas can be inspected.
            if (_debugView == DebugView.Off && _scatterStrength <= 0f)
                return;

            ApplyMaterialSettings(snapshot, renderingData.cameraData.camera);
            _scatterPass.Setup(snapshot);
            renderer.EnqueuePass(_scatterPass);
        }

        protected override void Dispose(bool disposing)
        {
            _scatterPass?.DisposePass();
            CoreUtils.Destroy(_scatterMaterial);
        }

        void ApplyMaterialSettings(AtmosRenderContext.Snapshot snapshot, Camera camera)
        {
            _scatterMaterial.SetTexture(AtmosPressureId, snapshot.Pressure);
            _scatterMaterial.SetTexture(AtmosMaskId, snapshot.Mask);

            if (snapshot.Temperature != null)
                _scatterMaterial.SetTexture(AtmosTemperatureId, snapshot.Temperature);
            if (snapshot.Composition != null)
                _scatterMaterial.SetTexture(AtmosCompositionId, snapshot.Composition);
            if (snapshot.FireIntensity != null)
                _scatterMaterial.SetTexture(AtmosFireId, snapshot.FireIntensity);

            _scatterMaterial.SetVector(AtmosAtlasBoundsId, snapshot.AtlasBounds);
            _scatterMaterial.SetFloat(AtmosVolumeHeightId, _volumeHeight);
            _scatterMaterial.SetFloat(AtmosReferencePressureId, _referencePressure);
            _scatterMaterial.SetFloat(AtmosFogPressureScaleId, _fogPressureScale);
            _scatterMaterial.SetFloat(AtmosScatterStrengthId, _scatterStrength);
            _scatterMaterial.SetColor(AtmosScatterColorId, _scatterColor);
            _scatterMaterial.SetInt(AtmosSlabStepsId, _slabSteps);
            _scatterMaterial.SetInt(AtmosDebugViewId, (int)_debugView);

            if (camera != null)
            {
                // Use a non-jittered inverse VP for stable depth->world reconstruction in
                // debug views when camera AA jitter is enabled (for example TAA).
                Matrix4x4 inverseViewProjection =
                    (camera.nonJitteredProjectionMatrix * camera.worldToCameraMatrix).inverse;
                _scatterMaterial.SetMatrix(AtmosInvViewProjId, inverseViewProjection);
            }
        }

        sealed class AtmosScatterPass : ScriptableRenderPass
        {
            static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            static readonly MaterialPropertyBlock s_PropertyBlock = new();

            readonly Material _material;
            AtmosRenderContext.Snapshot _snapshot;

            public AtmosScatterPass(Material material)
            {
                _material = material;
                profilingSampler = new ProfilingSampler("SS3D Atmos Scatter");
            }

            public void Setup(AtmosRenderContext.Snapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public void DisposePass()
            {
            }

            class PassData
            {
                public Material Material;
                public TextureHandle Source;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null || !_snapshot.Valid)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Sampling the back buffer as an input texture is unsupported; requiresIntermediateTexture
                // should prevent this, but bail out defensively.
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle activeColor = resourceData.activeColorTexture;
                if (!activeColor.IsValid())
                    return;

                // Copy the scene color into a temp texture (can't read+write the same target in one draw),
                // then run the scatter material as a raster pass that writes back into the active color
                // attachment. Mirrors URP FullScreenPassRendererFeature's input path, which reliably
                // survives render-graph culling because it writes the real camera color attachment.
                TextureDesc tempDesc = renderGraph.GetTextureDesc(activeColor);
                tempDesc.name = "_SS3DAtmosSceneCopy";
                tempDesc.clearBuffer = false;
                TextureHandle sceneCopy = renderGraph.CreateTexture(tempDesc);

                renderGraph.AddBlitPass(
                    activeColor,
                    sceneCopy,
                    Vector2.one,
                    Vector2.zero,
                    passName: "SS3D Atmos Copy Scene");

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "SS3D Atmos Scatter", out PassData passData, profilingSampler))
                {
                    passData.Material = _material;
                    passData.Source = sceneCopy;

                    builder.UseTexture(sceneCopy, AccessFlags.Read);
                    builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        s_PropertyBlock.Clear();
                        RTHandle source = data.Source;
                        if (source != null)
                            s_PropertyBlock.SetTexture(BlitTextureId, source);
                        s_PropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
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
            }
        }
    }
}
