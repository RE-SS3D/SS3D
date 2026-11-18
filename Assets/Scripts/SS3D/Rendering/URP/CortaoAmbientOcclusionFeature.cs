using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static SS3D.Rendering.URP.CortaoShaderIds;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Screen-space ambient occlusion with CORTAO (https://github.com/theagentd/CORTAO, MIT), replacing URP's
    /// SSAO. Runs after the depth-normals prepass and publishes <c>_SS3DOcclusionTexture</c> for the station
    /// shaders (<c>StationFlatLighting.hlsl</c>). The <see cref="StationShading"/> values are published by
    /// <see cref="StationShadingFeature"/>, so the look does not depend on this feature being on.
    /// </summary>
    /// <remarks>
    /// Deliberately not URP's <c>_SCREEN_SPACE_OCCLUSION</c> keyword: URP strips that keyword's variants from
    /// player builds unless its own SSAO feature is active, so the station shaders sample this texture directly.
    /// The four quality tiers share one renderer, so the per-tier cost settings live in
    /// <see cref="QualityLevels"/>, indexed by the active <see cref="QualitySettings"/> level.
    /// </remarks>
    public sealed class CortaoAmbientOcclusionFeature : ScriptableRendererFeature
    {
        public enum SampleQuality
        {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        /// <summary>What CORTAO costs at one <see cref="QualitySettings"/> level.</summary>
        [Serializable]
        public struct QualityLevelSettings
        {
            [Tooltip("The QualitySettings level this entry is for. Only labels the entry: levels are matched by "
                + "index, and CortaoQualityLevelTests checks the labels against QualitySettings.names.")]
            public string Level;

            [Tooltip("Off skips CORTAO and binds a white occlusion texture, so the station shaders read no AO.")]
            public bool Enabled;

            [Tooltip("Samples per pixel: Low 8, Medium 12, High 16.")]
            public SampleQuality Samples;

            [Tooltip("Run CORTAO and the blur on a quarter of the pixels, then upsample bilaterally.")]
            public bool HalfResolution;

            [Tooltip("Bilateral blur radius in screen pixels (halved in CORTAO pixels at half resolution); 0 "
                + "disables it. CORTAO's own noise is meant for TAA, which the player can turn off, and the "
                + "station look wants soft AO.")]
            [Range(0, 16)]
            public int BlurRadius;

            public QualityLevelSettings(string level, bool enabled, SampleQuality samples, bool halfResolution,
                int blurRadius)
            {
                Level = level;
                Enabled = enabled;
                Samples = samples;
                HalfResolution = halfResolution;
                BlurRadius = blurRadius;
            }
        }

        [SerializeField] private ComputeShader _prepassShader;
        [SerializeField] private ComputeShader _mainPassShader;
        [SerializeField] private ComputeShader _filterShader;
        [SerializeField] private ComputeShader _upsampleShader;

        [Tooltip("One entry per QualitySettings level, in order (Low, Medium, High, Ultra); levels past the end "
            + "use the last entry. Measured costs: Documents/architecture/systems/rendering-quality-tiers.md.")]
        [SerializeField] private QualityLevelSettings[] _qualityLevels = DefaultQualityLevels();

        [Tooltip("Maximum world-space occlusion radius, in metres.")]
        [SerializeField, Min(0.01f)] private float _radius = 0.75f;

        [Tooltip("Maximum sampling radius as a fraction of screen height. CORTAO recommends 0.1-0.25.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _maxScreenRadius = 0.2f;

        [Tooltip("Exponent on the occlusion term (1 = physically based, above 1 = darker).")]
        [SerializeField, Range(0.25f, 4f)] private float _intensity = 1f;

        [Tooltip("Packed depth mip bias. -4 is CORTAO's default; lower is more stable but slower.")]
        [SerializeField, Range(-6f, 0f)] private float _mipBias = -4f;

        [SerializeField, Range(1f, 8f)] private float _maxSlopeCompensation = 4f;
        [SerializeField, Range(0f, 1f)] private float _backfaceThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _backfaceBoost = 0f;

        [Tooltip("CORTAO's sparse prefilter. Meant to precede TAA, which SS3D's cameras use.")]
        [SerializeField] private bool _filter = true;

        [SerializeField, Min(0f)] private float _filterDepthSensitivity = 512f;
        [SerializeField, Min(0f)] private float _filterNormalSensitivity = 128f;

        [SerializeField] private ComputeShader _blurShader;

        [Tooltip("Blur and upsample: rejects samples off the centre pixel's surface plane (distance relative "
            + "to depth).")]
        [SerializeField, Min(0f)] private float _blurDepthSensitivity = 48f;

        [Tooltip("Blur and upsample: rejects samples whose normal differs from the centre pixel's.")]
        [SerializeField, Min(0f)] private float _blurNormalPower = 8f;

        private CortaoPass _pass;
        private GraphicsBuffer _occluderTable;

        /// <summary>Per-<see cref="QualitySettings"/>-level settings, in level order.</summary>
        public IReadOnlyList<QualityLevelSettings> QualityLevels => _qualityLevels;

        /// <summary>
        /// Low skips AO (it also has no fixture shadows), Medium runs 12 samples at half resolution, High and
        /// Ultra run the full-resolution 16-sample look.
        /// </summary>
        public static QualityLevelSettings[] DefaultQualityLevels() => new[]
        {
            new QualityLevelSettings("Low", false, SampleQuality.Low, true, 6),
            new QualityLevelSettings("Medium", true, SampleQuality.Medium, true, 6),
            new QualityLevelSettings("High", true, SampleQuality.High, false, 6),
            new QualityLevelSettings("Ultra", true, SampleQuality.High, false, 6),
        };

        /// <summary>The entry for a <see cref="QualitySettings"/> level; past the end, the last entry.</summary>
        public static QualityLevelSettings ResolveLevel(IReadOnlyList<QualityLevelSettings> levels, int qualityLevel)
        {
            if (levels == null || levels.Count == 0)
            {
                return new QualityLevelSettings("Fallback", true, SampleQuality.High, false, 6);
            }

            return levels[Mathf.Clamp(qualityLevel, 0, levels.Count - 1)];
        }

        public override void Create()
        {
            if (_prepassShader == null)
            {
                _prepassShader = Resources.Load<ComputeShader>("CORTAO/CortaoPrepass");
            }

            if (_mainPassShader == null)
            {
                _mainPassShader = Resources.Load<ComputeShader>("CORTAO/CortaoMainPass");
            }

            if (_filterShader == null)
            {
                _filterShader = Resources.Load<ComputeShader>("CORTAO/CortaoFilter");
            }

            if (_blurShader == null)
            {
                _blurShader = Resources.Load<ComputeShader>("CORTAO/CortaoBlur");
            }

            if (_upsampleShader == null)
            {
                _upsampleShader = Resources.Load<ComputeShader>("CORTAO/CortaoUpsample");
            }

            _pass ??= new CortaoPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1,
            };

            // No AO until a camera runs the passes; an unset global texture would read as grey.
            Shader.SetGlobalTexture(OcclusionTextureId, Texture2D.whiteTexture);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            StationShading shading = VolumeManager.instance.stack?.GetComponent<StationShading>();
            QualityLevelSettings level = ResolveLevel(_qualityLevels, QualitySettings.GetQualityLevel());
            if (!level.Enabled || !ShouldRunOcclusion(renderingData.cameraData, shading))
            {
                Shader.SetGlobalTexture(OcclusionTextureId, Texture2D.whiteTexture);
                return;
            }

            EnsureOccluderTable();
            _pass.Setup(this, _occluderTable, level);
            _pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _occluderTable?.Release();
            _occluderTable = null;
        }

        private bool ShouldRunOcclusion(CameraData cameraData, StationShading shading)
        {
            if (!SystemInfo.supportsComputeShaders
                || _prepassShader == null
                || _mainPassShader == null
                || _filterShader == null)
            {
                return false;
            }

            CameraType cameraType = cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
            {
                return false;
            }

            if (cameraData.renderType == CameraRenderType.Overlay || cameraData.camera.orthographic)
            {
                return false;
            }

            return shading == null || shading.UsesOcclusion();
        }

        private void EnsureOccluderTable()
        {
            if (_occluderTable != null && _occluderTable.IsValid())
            {
                return;
            }

            uint[] table = CortaoOccluderTable.Generate();
            _occluderTable = new GraphicsBuffer(GraphicsBuffer.Target.Structured, table.Length, sizeof(uint));
            _occluderTable.SetData(table);
        }

        private sealed class CortaoPass : ScriptableRenderPass
        {
            private const int MaxPackedMips = 8;

            private static readonly string[] MainKernelNames =
            {
                "CortaoMainPassLow",
                "CortaoMainPassMedium",
                "CortaoMainPassHigh",
            };

            private static GraphicsFormat? CachedOcclusionFormat;

            private readonly ProfilingSampler _prepassSampler = new("CORTAO Prepass");
            private readonly ProfilingSampler _mainSampler = new("CORTAO Main Pass");
            private readonly ProfilingSampler _filterSampler = new("CORTAO Filter");
            private readonly ProfilingSampler _blurSampler = new("CORTAO Bilateral Blur");
            private readonly ProfilingSampler _upsampleSampler = new("CORTAO Upsample");

            private CortaoAmbientOcclusionFeature _feature;
            private GraphicsBuffer _occluderTable;
            private QualityLevelSettings _level;

            private sealed class PrepassData
            {
                public ComputeShader Shader;
                public int Kernel;
                public TextureHandle Depth;
                public TextureHandle Normals;
                public TextureHandle Packed;
                public Matrix4x4 WorldToView;
                public int PackedResolution;
                public int InputScale;
                public float DepthScale;
                public float DepthOffset;
                public int RandomSeed;
                public int MipCount;
                public int GroupsX;
                public int GroupsY;
            }

            private sealed class MainPassData
            {
                public ComputeShader Shader;
                public int Kernel;
                public TextureHandle Packed;
                public TextureHandle Normals;
                public TextureHandle Output;
                public GraphicsBuffer OccluderTable;
                public Matrix4x4 WorldToView;
                public int PackedResolution;
                public int InputScale;
                public int RandomSeed;
                public float NormalBias;
                public float Radius;
                public Vector4 UnprojectParams;
                public Vector4 ProjectionParams;
                public float MaxApparentCircleRadiusCoefficient;
                public float MipBias;
                public float InverseMaxSlopeCompensation;
                public float BackfaceThreshold;
                public float BackfaceBoost;
                public float MaxMip;
                public float Intensity;
                public int GroupsX;
                public int GroupsY;
            }

            private sealed class FilterData
            {
                public ComputeShader Shader;
                public int Kernel;
                public TextureHandle Packed;
                public TextureHandle Input;
                public TextureHandle Output;
                public int PackedResolution;
                public float DepthSensitivity;
                public float NormalSensitivity;
                public Vector4 TexelSize;
                public int GroupsX;
                public int GroupsY;
            }

            private sealed class BlurData
            {
                public ComputeShader Shader;
                public int Kernel;
                public TextureHandle Packed;
                public TextureHandle Input;
                public TextureHandle Output;
                public int PackedResolution;
                public Vector4 UnprojectParams;
                public Vector4 Direction;
                public int Radius;
                public float DepthSensitivity;
                public float NormalPower;
                public int GroupsX;
                public int GroupsY;
            }

            private sealed class UpsampleData
            {
                public ComputeShader Shader;
                public int Kernel;
                public TextureHandle Depth;
                public TextureHandle Normals;
                public TextureHandle Packed;
                public TextureHandle Input;
                public TextureHandle Output;
                public Matrix4x4 WorldToView;
                public int PackedResolution;
                public int OutputResolution;
                public float DepthScale;
                public float DepthOffset;
                public Vector4 UnprojectParams;
                public float DepthSensitivity;
                public float NormalPower;
                public int GroupsX;
                public int GroupsY;
            }

            public void Setup(CortaoAmbientOcclusionFeature feature, GraphicsBuffer occluderTable,
                QualityLevelSettings level)
            {
                _feature = feature;
                _occluderTable = occluderTable;
                _level = level;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle depth = resourceData.cameraDepthTexture;
                TextureHandle normals = resourceData.cameraNormalsTexture;
                if (_feature == null || !depth.IsValid() || !normals.IsValid())
                {
                    Shader.SetGlobalTexture(OcclusionTextureId, Texture2D.whiteTexture);
                    return;
                }

                TextureDesc depthDesc = renderGraph.GetTextureDesc(depth);
                int width = depthDesc.width;
                int height = depthDesc.height;
                if (width <= 0 || height <= 0 || width > 0xFFFF || height > 0xFFFF)
                {
                    Shader.SetGlobalTexture(OcclusionTextureId, Texture2D.whiteTexture);
                    return;
                }

                Camera camera = cameraData.camera;
                Matrix4x4 worldToView = cameraData.GetViewMatrix();
                // Non-GPU (OpenGL-convention) projection, including the TAA jitter. URP renders the depth and
                // normals textures flipped on UV-starts-at-top APIs, so texture rows already follow NDC +Y and
                // CORTAO's flipY stays false on every platform.
                Matrix4x4 projection = cameraData.GetProjectionMatrix();
                Matrix4x4 inverseProjection = projection.inverse;

                // CORTAO's own resolution: the screen, or every other pixel on each axis at half resolution
                // (the prepass and main pass read screen pixel 2p, see CortaoCommon.hlsl).
                bool halfResolution = _level.HalfResolution && _feature._upsampleShader != null;
                int inputScale = halfResolution ? 2 : 1;
                int aoWidth = DivideRoundUp(width, inputScale);
                int aoHeight = DivideRoundUp(height, inputScale);

                int packedResolution = aoWidth | (aoHeight << 16);
                int mipCount = GetPackedMipCount(aoWidth, aoHeight);
                int randomSeed = GetRandomSeed(cameraData);
                GetDepthLinearization(camera, out float depthScale, out float depthOffset);

                TextureHandle packed = CreatePackedTexture(renderGraph, aoWidth, aoHeight);
                TextureHandle rawOcclusion = CreateOcclusionTexture(renderGraph, aoWidth, aoHeight, "_CortaoOcclusionRaw");

                RecordPrepass(renderGraph, depth, normals, packed, worldToView, depthScale, depthOffset,
                    packedResolution, inputScale, randomSeed, mipCount, aoWidth, aoHeight);

                bool filter = _feature._filter;
                // The blur radius is in screen pixels; at half resolution each CORTAO pixel spans two.
                int blurRadius = (_level.BlurRadius + inputScale - 1) / inputScale;
                bool blur = blurRadius > 0 && _feature._blurShader != null;
                Vector4 unprojectParams = GetUnprojectParams(inverseProjection);

                // Only the last pass in the chain publishes _SS3DOcclusionTexture.
                TextureHandle occlusion = rawOcclusion;
                RecordMainPass(renderGraph, packed, normals, rawOcclusion, worldToView, projection, inverseProjection,
                    unprojectParams, packedResolution, inputScale, randomSeed, mipCount, aoWidth, aoHeight,
                    publish: !filter && !blur && !halfResolution);

                if (filter)
                {
                    TextureHandle filtered =
                        CreateOcclusionTexture(renderGraph, aoWidth, aoHeight, "_CortaoOcclusionFiltered");
                    RecordFilter(renderGraph, packed, occlusion, filtered, packedResolution, aoWidth, aoHeight,
                        publish: !blur && !halfResolution);
                    occlusion = filtered;
                }

                if (blur)
                {
                    TextureHandle horizontal =
                        CreateOcclusionTexture(renderGraph, aoWidth, aoHeight, "_CortaoOcclusionBlurH");
                    RecordBlur(renderGraph, packed, occlusion, horizontal, new Vector4(1f, 0f, 0f, 0f),
                        unprojectParams, packedResolution, blurRadius, aoWidth, aoHeight, publish: false);

                    TextureHandle vertical = CreateOcclusionTexture(renderGraph, aoWidth, aoHeight,
                        halfResolution ? "_CortaoOcclusionBlurV" : "_CortaoOcclusion");
                    RecordBlur(renderGraph, packed, horizontal, vertical, new Vector4(0f, 1f, 0f, 0f),
                        unprojectParams, packedResolution, blurRadius, aoWidth, aoHeight, publish: !halfResolution);
                    occlusion = vertical;
                }

                if (halfResolution)
                {
                    TextureHandle upsampled = CreateOcclusionTexture(renderGraph, width, height, "_CortaoOcclusion");
                    RecordUpsample(renderGraph, depth, normals, packed, occlusion, upsampled, worldToView, depthScale,
                        depthOffset, unprojectParams, packedResolution, width, height);
                }
            }

            // CORTAO linearises depth as 1 / (depth * scale + offset); these match URP's _ZBufferParams.zw.
            private static void GetDepthLinearization(Camera camera, out float scale, out float offset)
            {
                float near = camera.nearClipPlane;
                float far = camera.farClipPlane;
                if (SystemInfo.usesReversedZBuffer)
                {
                    scale = (far - near) / (near * far);
                    offset = 1f / far;
                }
                else
                {
                    scale = (near - far) / (near * far);
                    offset = 1f / near;
                }
            }

            // Port of CORTAOParameters.h unproject parameters (flipY = false).
            private static Vector4 GetUnprojectParams(Matrix4x4 inverseProjection)
            {
                float invX = inverseProjection.m00;
                float invY = inverseProjection.m11;
                return new Vector4(2f * invX, 2f * invY, -invX + inverseProjection.m03, -invY + inverseProjection.m13);
            }

            private void RecordPrepass(
                RenderGraph renderGraph,
                TextureHandle depth,
                TextureHandle normals,
                TextureHandle packed,
                Matrix4x4 worldToView,
                float depthScale,
                float depthOffset,
                int packedResolution,
                int inputScale,
                int randomSeed,
                int mipCount,
                int width,
                int height)
            {
                using IComputeRenderGraphBuilder builder =
                    renderGraph.AddComputePass("CORTAO Prepass", out PrepassData data, _prepassSampler);

                data.Shader = _feature._prepassShader;
                data.Kernel = data.Shader.FindKernel("CortaoPrepass");
                data.Depth = depth;
                data.Normals = normals;
                data.Packed = packed;
                data.WorldToView = worldToView;
                data.PackedResolution = packedResolution;
                data.InputScale = inputScale;
                data.DepthScale = depthScale;
                data.DepthOffset = depthOffset;
                data.RandomSeed = randomSeed;
                data.MipCount = mipCount;
                data.GroupsX = DivideRoundUp(width, 8);
                data.GroupsY = DivideRoundUp(height, 8);

                builder.UseTexture(depth, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.UseTexture(packed, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PrepassData d, ComputeGraphContext context) =>
                {
                    ComputeCommandBuffer cmd = context.cmd;
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, DepthTextureId, d.Depth);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, NormalsTextureId, d.Normals);
                    for (int mip = 0; mip < MaxPackedMips; mip++)
                    {
                        // Unused slots alias the last real mip; the shader returns before writing them.
                        int boundMip = Mathf.Min(mip, d.MipCount - 1);
                        cmd.SetComputeTextureParam(d.Shader, d.Kernel, PackedMipIds[mip], d.Packed, boundMip);
                    }

                    cmd.SetComputeMatrixParam(d.Shader, WorldToViewId, d.WorldToView);
                    cmd.SetComputeIntParam(d.Shader, PackedResolutionId, d.PackedResolution);
                    cmd.SetComputeIntParam(d.Shader, InputScaleId, d.InputScale);
                    cmd.SetComputeFloatParam(d.Shader, DepthScaleId, d.DepthScale);
                    cmd.SetComputeFloatParam(d.Shader, DepthOffsetId, d.DepthOffset);
                    cmd.SetComputeIntParam(d.Shader, RandomSeedId, d.RandomSeed);
                    cmd.SetComputeIntParam(d.Shader, MipCountId, d.MipCount);
                    cmd.DispatchCompute(d.Shader, d.Kernel, d.GroupsX, d.GroupsY, 1);
                });
            }

            private void RecordMainPass(
                RenderGraph renderGraph,
                TextureHandle packed,
                TextureHandle normals,
                TextureHandle output,
                Matrix4x4 worldToView,
                Matrix4x4 projection,
                Matrix4x4 inverseProjection,
                Vector4 unprojectParams,
                int packedResolution,
                int inputScale,
                int randomSeed,
                int mipCount,
                int width,
                int height,
                bool publish)
            {
                using IComputeRenderGraphBuilder builder =
                    renderGraph.AddComputePass("CORTAO Main Pass", out MainPassData data, _mainSampler);

                CortaoAmbientOcclusionFeature feature = _feature;
                float invX = inverseProjection.m00;
                float invY = inverseProjection.m11;
                float maxApparentCircleRadius = invY * feature._maxScreenRadius * 2f;

                data.Shader = feature._mainPassShader;
                data.Kernel = data.Shader.FindKernel(MainKernelNames[(int)_level.Samples]);
                data.Packed = packed;
                data.Normals = normals;
                data.Output = output;
                data.OccluderTable = _occluderTable;
                data.WorldToView = worldToView;
                data.PackedResolution = packedResolution;
                data.InputScale = inputScale;
                data.RandomSeed = randomSeed;
                // Port of CORTAOParameters.h populateMainPassParameters (flipY = false).
                data.NormalBias = invX / width * Mathf.Sqrt(2f);
                data.Radius = feature._radius;
                data.UnprojectParams = unprojectParams;
                data.ProjectionParams = new Vector4(
                    0.5f * projection.m00,
                    0.5f * projection.m11,
                    -0.5f + 0.5f * projection.m02,
                    -0.5f + 0.5f * projection.m12);
                data.MaxApparentCircleRadiusCoefficient = maxApparentCircleRadius
                    / Mathf.Sqrt(maxApparentCircleRadius * maxApparentCircleRadius + 1f);
                data.MipBias = Mathf.Log(projection.m11 * height * 0.5f, 2f) + feature._mipBias;
                data.InverseMaxSlopeCompensation = 1f / feature._maxSlopeCompensation;
                data.BackfaceThreshold = feature._backfaceThreshold;
                data.BackfaceBoost = feature._backfaceBoost;
                data.MaxMip = mipCount - 1;
                data.Intensity = feature._intensity;
                data.GroupsX = DivideRoundUp(width, 16);
                data.GroupsY = DivideRoundUp(height, 16);

                builder.UseTexture(packed, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.UseTexture(output, AccessFlags.Write);
                builder.AllowPassCulling(false);
                if (publish)
                {
                    builder.SetGlobalTextureAfterPass(output, OcclusionTextureId);
                }

                builder.SetRenderFunc(static (MainPassData d, ComputeGraphContext context) =>
                {
                    ComputeCommandBuffer cmd = context.cmd;
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, PackedDataId, d.Packed);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, NormalsTextureId, d.Normals);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionOutputId, d.Output);
                    cmd.SetComputeBufferParam(d.Shader, d.Kernel, OccluderTableId, d.OccluderTable);
                    cmd.SetComputeMatrixParam(d.Shader, WorldToViewId, d.WorldToView);
                    cmd.SetComputeIntParam(d.Shader, PackedResolutionId, d.PackedResolution);
                    cmd.SetComputeIntParam(d.Shader, InputScaleId, d.InputScale);
                    cmd.SetComputeIntParam(d.Shader, RandomSeedId, d.RandomSeed);
                    cmd.SetComputeFloatParam(d.Shader, NormalBiasId, d.NormalBias);
                    cmd.SetComputeFloatParam(d.Shader, RadiusId, d.Radius);
                    cmd.SetComputeVectorParam(d.Shader, UnprojectParamsId, d.UnprojectParams);
                    cmd.SetComputeVectorParam(d.Shader, ProjectionParamsId, d.ProjectionParams);
                    cmd.SetComputeFloatParam(d.Shader, MaxApparentCircleRadiusCoefficientId,
                        d.MaxApparentCircleRadiusCoefficient);
                    cmd.SetComputeFloatParam(d.Shader, MipBiasId, d.MipBias);
                    cmd.SetComputeFloatParam(d.Shader, InverseMaxSlopeCompensationId, d.InverseMaxSlopeCompensation);
                    cmd.SetComputeFloatParam(d.Shader, BackfaceThresholdId, d.BackfaceThreshold);
                    cmd.SetComputeFloatParam(d.Shader, BackfaceBoostId, d.BackfaceBoost);
                    cmd.SetComputeFloatParam(d.Shader, MaxMipId, d.MaxMip);
                    cmd.SetComputeFloatParam(d.Shader, IntensityId, d.Intensity);
                    cmd.DispatchCompute(d.Shader, d.Kernel, d.GroupsX, d.GroupsY, 1);
                });
            }

            private void RecordFilter(
                RenderGraph renderGraph,
                TextureHandle packed,
                TextureHandle input,
                TextureHandle output,
                int packedResolution,
                int width,
                int height,
                bool publish)
            {
                using IComputeRenderGraphBuilder builder =
                    renderGraph.AddComputePass("CORTAO Filter", out FilterData data, _filterSampler);

                data.Shader = _feature._filterShader;
                data.Kernel = data.Shader.FindKernel("CortaoFilter");
                data.Packed = packed;
                data.Input = input;
                data.Output = output;
                data.PackedResolution = packedResolution;
                data.DepthSensitivity = _feature._filterDepthSensitivity;
                data.NormalSensitivity = _feature._filterNormalSensitivity;
                data.TexelSize = new Vector4(1f / width, 1f / height, width, height);
                // One thread per 2x2 block.
                data.GroupsX = DivideRoundUp(DivideRoundUp(width, 2), 8);
                data.GroupsY = DivideRoundUp(DivideRoundUp(height, 2), 8);

                builder.UseTexture(packed, AccessFlags.Read);
                builder.UseTexture(input, AccessFlags.Read);
                builder.UseTexture(output, AccessFlags.Write);
                builder.AllowPassCulling(false);
                if (publish)
                {
                    builder.SetGlobalTextureAfterPass(output, OcclusionTextureId);
                }

                builder.SetRenderFunc(static (FilterData d, ComputeGraphContext context) =>
                {
                    ComputeCommandBuffer cmd = context.cmd;
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, PackedDataId, d.Packed);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionInputId, d.Input);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionOutputId, d.Output);
                    cmd.SetComputeIntParam(d.Shader, PackedResolutionId, d.PackedResolution);
                    cmd.SetComputeFloatParam(d.Shader, DepthSensitivityId, d.DepthSensitivity);
                    cmd.SetComputeFloatParam(d.Shader, NormalSensitivityId, d.NormalSensitivity);
                    cmd.SetComputeVectorParam(d.Shader, TexelSizeId, d.TexelSize);
                    cmd.DispatchCompute(d.Shader, d.Kernel, d.GroupsX, d.GroupsY, 1);
                });
            }

            private void RecordBlur(
                RenderGraph renderGraph,
                TextureHandle packed,
                TextureHandle input,
                TextureHandle output,
                Vector4 direction,
                Vector4 unprojectParams,
                int packedResolution,
                int radius,
                int width,
                int height,
                bool publish)
            {
                using IComputeRenderGraphBuilder builder =
                    renderGraph.AddComputePass("CORTAO Bilateral Blur", out BlurData data, _blurSampler);

                data.Shader = _feature._blurShader;
                data.Kernel = data.Shader.FindKernel("CortaoBlur");
                data.Packed = packed;
                data.Input = input;
                data.Output = output;
                data.PackedResolution = packedResolution;
                data.UnprojectParams = unprojectParams;
                data.Direction = direction;
                data.Radius = radius;
                data.DepthSensitivity = _feature._blurDepthSensitivity;
                data.NormalPower = _feature._blurNormalPower;
                data.GroupsX = DivideRoundUp(width, 8);
                data.GroupsY = DivideRoundUp(height, 8);

                builder.UseTexture(packed, AccessFlags.Read);
                builder.UseTexture(input, AccessFlags.Read);
                builder.UseTexture(output, AccessFlags.Write);
                builder.AllowPassCulling(false);
                if (publish)
                {
                    builder.SetGlobalTextureAfterPass(output, OcclusionTextureId);
                }

                builder.SetRenderFunc(static (BlurData d, ComputeGraphContext context) =>
                {
                    ComputeCommandBuffer cmd = context.cmd;
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, PackedDataId, d.Packed);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionInputId, d.Input);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionOutputId, d.Output);
                    cmd.SetComputeIntParam(d.Shader, PackedResolutionId, d.PackedResolution);
                    cmd.SetComputeVectorParam(d.Shader, UnprojectParamsId, d.UnprojectParams);
                    cmd.SetComputeVectorParam(d.Shader, BlurDirectionId, d.Direction);
                    cmd.SetComputeIntParam(d.Shader, BlurRadiusId, d.Radius);
                    cmd.SetComputeFloatParam(d.Shader, BlurDepthSensitivityId, d.DepthSensitivity);
                    cmd.SetComputeFloatParam(d.Shader, BlurNormalPowerId, d.NormalPower);
                    cmd.DispatchCompute(d.Shader, d.Kernel, d.GroupsX, d.GroupsY, 1);
                });
            }

            private void RecordUpsample(
                RenderGraph renderGraph,
                TextureHandle depth,
                TextureHandle normals,
                TextureHandle packed,
                TextureHandle input,
                TextureHandle output,
                Matrix4x4 worldToView,
                float depthScale,
                float depthOffset,
                Vector4 unprojectParams,
                int packedResolution,
                int width,
                int height)
            {
                using IComputeRenderGraphBuilder builder =
                    renderGraph.AddComputePass("CORTAO Upsample", out UpsampleData data, _upsampleSampler);

                data.Shader = _feature._upsampleShader;
                data.Kernel = data.Shader.FindKernel("CortaoUpsample");
                data.Depth = depth;
                data.Normals = normals;
                data.Packed = packed;
                data.Input = input;
                data.Output = output;
                data.WorldToView = worldToView;
                data.PackedResolution = packedResolution;
                data.OutputResolution = width | (height << 16);
                data.DepthScale = depthScale;
                data.DepthOffset = depthOffset;
                data.UnprojectParams = unprojectParams;
                data.DepthSensitivity = _feature._blurDepthSensitivity;
                data.NormalPower = _feature._blurNormalPower;
                data.GroupsX = DivideRoundUp(width, 8);
                data.GroupsY = DivideRoundUp(height, 8);

                builder.UseTexture(depth, AccessFlags.Read);
                builder.UseTexture(normals, AccessFlags.Read);
                builder.UseTexture(packed, AccessFlags.Read);
                builder.UseTexture(input, AccessFlags.Read);
                builder.UseTexture(output, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetGlobalTextureAfterPass(output, OcclusionTextureId);

                builder.SetRenderFunc(static (UpsampleData d, ComputeGraphContext context) =>
                {
                    ComputeCommandBuffer cmd = context.cmd;
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, DepthTextureId, d.Depth);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, NormalsTextureId, d.Normals);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, PackedDataId, d.Packed);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionInputId, d.Input);
                    cmd.SetComputeTextureParam(d.Shader, d.Kernel, OcclusionOutputId, d.Output);
                    cmd.SetComputeMatrixParam(d.Shader, WorldToViewId, d.WorldToView);
                    cmd.SetComputeIntParam(d.Shader, PackedResolutionId, d.PackedResolution);
                    cmd.SetComputeIntParam(d.Shader, OutputResolutionId, d.OutputResolution);
                    cmd.SetComputeFloatParam(d.Shader, DepthScaleId, d.DepthScale);
                    cmd.SetComputeFloatParam(d.Shader, DepthOffsetId, d.DepthOffset);
                    cmd.SetComputeVectorParam(d.Shader, UnprojectParamsId, d.UnprojectParams);
                    cmd.SetComputeFloatParam(d.Shader, BlurDepthSensitivityId, d.DepthSensitivity);
                    cmd.SetComputeFloatParam(d.Shader, BlurNormalPowerId, d.NormalPower);
                    cmd.DispatchCompute(d.Shader, d.Kernel, d.GroupsX, d.GroupsY, 1);
                });
            }

            private static TextureHandle CreatePackedTexture(RenderGraph renderGraph, int width, int height)
            {
                TextureDesc desc = new(width, height)
                {
                    name = "_CortaoPackedData",
                    // Float view of packed uint bits: CORTAO's SM 6.7 workaround (see CortaoPrepass.compute).
                    format = GraphicsFormat.R32_SFloat,
                    useMipMap = true,
                    autoGenerateMips = false,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = false,
                };
                return renderGraph.CreateTexture(desc);
            }

            private static TextureHandle CreateOcclusionTexture(RenderGraph renderGraph, int width, int height, string name)
            {
                TextureDesc desc = new(width, height)
                {
                    name = name,
                    format = GetOcclusionFormat(),
                    enableRandomWrite = true,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = false,
                };
                return renderGraph.CreateTexture(desc);
            }

            // OpenGL and Vulkan compile the compute passes' RWTexture2D<float> to an r32f image, which must match
            // the bound texture's format; D3D and Metal convert typed stores, so R8 is enough there.
            private static GraphicsFormat GetOcclusionFormat()
            {
                if (CachedOcclusionFormat.HasValue)
                {
                    return CachedOcclusionFormat.Value;
                }

                GraphicsDeviceType api = SystemInfo.graphicsDeviceType;
                bool formatQualifiedImages = api == GraphicsDeviceType.OpenGLCore
                    || api == GraphicsDeviceType.OpenGLES3
                    || api == GraphicsDeviceType.Vulkan;
                if (formatQualifiedImages)
                {
                    CachedOcclusionFormat = GraphicsFormat.R32_SFloat;
                }
                else
                {
                    CachedOcclusionFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.LoadStore)
                        ? GraphicsFormat.R8_UNorm
                        : GraphicsFormat.R16_SFloat;
                }

                return CachedOcclusionFormat.Value;
            }

            // CORTAO samples coarser mips for distant occluders; ~64 px on the short side is enough.
            private static int GetPackedMipCount(int width, int height)
            {
                int fullChain = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(width, height), 2f));
                int wanted = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Min(width, height) / 64f, 2f));
                return Mathf.Clamp(wanted, 1, Mathf.Min(MaxPackedMips, fullChain));
            }

            // CORTAO expects a new seed every frame for TAA to resolve the noise, and a constant one otherwise.
            private static int GetRandomSeed(UniversalCameraData cameraData)
            {
                if (cameraData.antialiasing != AntialiasingMode.TemporalAntiAliasing)
                {
                    return unchecked((int)0x9E3779B9u);
                }

                uint seed = (uint)Time.frameCount * 747796405u + 2891336453u;
                seed = ((seed >> (int)((seed >> 28) + 4u)) ^ seed) * 277803737u;
                return unchecked((int)((seed >> 22) ^ seed));
            }

            private static int DivideRoundUp(int value, int divisor)
            {
                return (value + divisor - 1) / divisor;
            }
        }
    }
}
