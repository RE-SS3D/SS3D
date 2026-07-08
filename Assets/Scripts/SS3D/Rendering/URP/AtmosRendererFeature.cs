using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// URP feature that composites turf gas scattering and emission over the camera color buffer.
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
        private static readonly int AtmosGlowStrengthId = Shader.PropertyToID("_AtmosGlowStrength");
        private static readonly int AtmosPlasmaEmissionColorId = Shader.PropertyToID("_AtmosPlasmaEmissionColor");
        private static readonly int AtmosIgnitionTemperatureId = Shader.PropertyToID("_AtmosIgnitionTemperature");

        [SerializeField] private Shader _scatterShader;
        [SerializeField] private Shader _glowShader;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private DebugView _debugView = DebugView.Off;
        [SerializeField] private float _scatterStrength = 1f;
        [SerializeField] private Color _scatterColor = new(0.65f, 0.7f, 0.75f, 1f);
        [SerializeField] private float _glowStrength = 1f;
        [SerializeField] private Color _plasmaEmissionColor = new(0.75f, 0.2f, 1f, 1f);
        [SerializeField] private float _volumeHeight = 2.5f;
        [SerializeField] private float _referencePressure = 101.325f;
        [SerializeField] private float _fogPressureScale = 80f;
        [SerializeField] [Range(1, 8)] private int _slabSteps = 6;

        AtmosFullscreenPass _scatterPass;
        AtmosFullscreenPass _glowPass;
        Material _scatterMaterial;
        Material _glowMaterial;

        public override void Create()
        {
            if (_scatterShader == null)
                _scatterShader = Shader.Find("Custom/AtmosScatter");
            if (_glowShader == null)
                _glowShader = Shader.Find("Custom/AtmosGlow");

            if (_scatterShader != null && _scatterMaterial == null)
                _scatterMaterial = CoreUtils.CreateEngineMaterial(_scatterShader);
            if (_glowShader != null && _glowMaterial == null)
                _glowMaterial = CoreUtils.CreateEngineMaterial(_glowShader);

            _scatterPass = new AtmosFullscreenPass(_scatterMaterial, "SS3D Atmos Scatter", "SS3D Atmos Copy Scene")
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
                requiresIntermediateTexture = true,
            };
            _glowPass = new AtmosFullscreenPass(_glowMaterial, "SS3D Atmos Glow", "SS3D Atmos Copy Scene Glow")
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
                requiresIntermediateTexture = true,
            };

            _scatterPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            _glowPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_enabled)
                return;

            if (!AtmosRenderContext.TryGetRequest(out AtmosRenderContext.Request request))
                return;

            if (renderingData.cameraData.camera != request.SourceCamera)
                return;

            if (!AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot))
                return;

            if (snapshot.Pressure == null || snapshot.Mask == null)
                return;

            bool debugActive = _debugView != DebugView.Off;
            bool scatterActive = debugActive || _scatterStrength > 0f;
            bool glowActive = !debugActive && _glowStrength > 0f
                && snapshot.Temperature != null
                && snapshot.Composition != null
                && snapshot.FireIntensity != null;

            if (!scatterActive && !glowActive)
                return;

            ApplySharedMaterialSettings(snapshot, renderingData.cameraData.camera);

            if (scatterActive && _scatterMaterial != null)
            {
                ApplyScatterMaterialSettings();
                _scatterPass.Setup(snapshot);
                renderer.EnqueuePass(_scatterPass);
            }

            if (glowActive && _glowMaterial != null)
            {
                ApplyGlowMaterialSettings(snapshot);
                _glowPass.Setup(snapshot);
                renderer.EnqueuePass(_glowPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _scatterPass?.DisposePass();
            _glowPass?.DisposePass();
            CoreUtils.Destroy(_scatterMaterial);
            CoreUtils.Destroy(_glowMaterial);
        }

        void ApplySharedMaterialSettings(AtmosRenderContext.Snapshot snapshot, Camera camera)
        {
            Material[] materials = { _scatterMaterial, _glowMaterial };
            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                material.SetTexture(AtmosPressureId, snapshot.Pressure);
                material.SetTexture(AtmosMaskId, snapshot.Mask);

                if (snapshot.Temperature != null)
                    material.SetTexture(AtmosTemperatureId, snapshot.Temperature);
                if (snapshot.Composition != null)
                    material.SetTexture(AtmosCompositionId, snapshot.Composition);
                if (snapshot.FireIntensity != null)
                    material.SetTexture(AtmosFireId, snapshot.FireIntensity);

                material.SetVector(AtmosAtlasBoundsId, snapshot.AtlasBounds);
                material.SetFloat(AtmosVolumeHeightId, _volumeHeight);
                material.SetFloat(AtmosReferencePressureId, _referencePressure);
                material.SetFloat(AtmosFogPressureScaleId, _fogPressureScale);
                material.SetInt(AtmosSlabStepsId, _slabSteps);

                if (camera != null)
                {
                    Matrix4x4 inverseViewProjection =
                        (camera.nonJitteredProjectionMatrix * camera.worldToCameraMatrix).inverse;
                    material.SetMatrix(AtmosInvViewProjId, inverseViewProjection);
                }
            }
        }

        void ApplyScatterMaterialSettings()
        {
            _scatterMaterial.SetFloat(AtmosScatterStrengthId, _scatterStrength);
            _scatterMaterial.SetColor(AtmosScatterColorId, _scatterColor);
            _scatterMaterial.SetInt(AtmosDebugViewId, (int)_debugView);
        }

        void ApplyGlowMaterialSettings(AtmosRenderContext.Snapshot snapshot)
        {
            _glowMaterial.SetFloat(AtmosGlowStrengthId, _glowStrength);
            _glowMaterial.SetColor(AtmosPlasmaEmissionColorId, _plasmaEmissionColor);
            _glowMaterial.SetFloat(AtmosIgnitionTemperatureId, snapshot.IgnitionTemperature);
            _glowMaterial.SetInt(AtmosDebugViewId, 0);
        }

        sealed class AtmosFullscreenPass : ScriptableRenderPass
        {
            static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            static readonly MaterialPropertyBlock s_PropertyBlock = new();

            readonly Material _material;
            readonly string _drawPassName;
            readonly string _copyPassName;
            AtmosRenderContext.Snapshot _snapshot;

            public AtmosFullscreenPass(Material material, string drawPassName, string copyPassName)
            {
                _material = material;
                _drawPassName = drawPassName;
                _copyPassName = copyPassName;
                profilingSampler = new ProfilingSampler(drawPassName);
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

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle activeColor = resourceData.activeColorTexture;
                if (!activeColor.IsValid())
                    return;

                TextureDesc tempDesc = renderGraph.GetTextureDesc(activeColor);
                tempDesc.name = "_SS3DAtmosSceneCopy";
                tempDesc.clearBuffer = false;
                TextureHandle sceneCopy = renderGraph.CreateTexture(tempDesc);

                renderGraph.AddBlitPass(
                    activeColor,
                    sceneCopy,
                    Vector2.one,
                    Vector2.zero,
                    passName: _copyPassName);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    _drawPassName, out PassData passData, profilingSampler))
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
