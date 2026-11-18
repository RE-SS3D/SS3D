using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Shader property IDs for <see cref="CortaoAmbientOcclusionFeature"/>'s compute passes
    /// (<c>Assets/Content/Resources/CORTAO/*.compute</c>) and the occlusion texture it publishes.
    /// </summary>
    internal static class CortaoShaderIds
    {
        /// <summary>The finished occlusion, sampled by the station shaders (white when AO is off).</summary>
        internal static readonly int OcclusionTextureId = Shader.PropertyToID("_SS3DOcclusionTexture");

        internal static readonly int DepthTextureId = Shader.PropertyToID("_CortaoDepthTexture");
        internal static readonly int NormalsTextureId = Shader.PropertyToID("_CortaoNormalsTexture");
        internal static readonly int WorldToViewId = Shader.PropertyToID("_CortaoWorldToView");
        internal static readonly int PackedResolutionId = Shader.PropertyToID("_CortaoPackedResolution");
        internal static readonly int DepthScaleId = Shader.PropertyToID("_CortaoDepthScale");
        internal static readonly int DepthOffsetId = Shader.PropertyToID("_CortaoDepthOffset");
        internal static readonly int RandomSeedId = Shader.PropertyToID("_CortaoRandomSeed");
        internal static readonly int MipCountId = Shader.PropertyToID("_CortaoMipCount");
        internal static readonly int PackedDataId = Shader.PropertyToID("_CortaoPackedData");
        internal static readonly int OccluderTableId = Shader.PropertyToID("_CortaoOccluderTable");
        internal static readonly int OcclusionInputId = Shader.PropertyToID("_CortaoOcclusionInput");
        internal static readonly int OcclusionOutputId = Shader.PropertyToID("_CortaoOcclusionOutput");
        internal static readonly int NormalBiasId = Shader.PropertyToID("_CortaoNormalBias");
        internal static readonly int RadiusId = Shader.PropertyToID("_CortaoRadius");
        internal static readonly int UnprojectParamsId = Shader.PropertyToID("_CortaoUnprojectParams");
        internal static readonly int ProjectionParamsId = Shader.PropertyToID("_CortaoProjectionParams");
        internal static readonly int MaxApparentCircleRadiusCoefficientId =
            Shader.PropertyToID("_CortaoMaxApparentCircleRadiusCoefficient");
        internal static readonly int MipBiasId = Shader.PropertyToID("_CortaoMipBias");
        internal static readonly int InverseMaxSlopeCompensationId =
            Shader.PropertyToID("_CortaoInverseMaxSlopeCompensation");
        internal static readonly int BackfaceThresholdId = Shader.PropertyToID("_CortaoBackfaceThreshold");
        internal static readonly int BackfaceBoostId = Shader.PropertyToID("_CortaoBackfaceBoost");
        internal static readonly int MaxMipId = Shader.PropertyToID("_CortaoMaxMip");
        internal static readonly int IntensityId = Shader.PropertyToID("_CortaoIntensity");
        internal static readonly int DepthSensitivityId = Shader.PropertyToID("_CortaoDepthSensitivity");
        internal static readonly int NormalSensitivityId = Shader.PropertyToID("_CortaoNormalSensitivity");
        internal static readonly int TexelSizeId = Shader.PropertyToID("_CortaoTexelSize");
        internal static readonly int BlurDirectionId = Shader.PropertyToID("_CortaoBlurDirection");
        internal static readonly int BlurRadiusId = Shader.PropertyToID("_CortaoBlurRadius");
        internal static readonly int BlurDepthSensitivityId = Shader.PropertyToID("_CortaoBlurDepthSensitivity");
        internal static readonly int BlurNormalPowerId = Shader.PropertyToID("_CortaoBlurNormalPower");
        internal static readonly int InputScaleId = Shader.PropertyToID("_CortaoInputScale");
        internal static readonly int OutputResolutionId = Shader.PropertyToID("_CortaoOutputResolution");

        internal static readonly int[] PackedMipIds =
        {
            Shader.PropertyToID("_CortaoPackedMip0"),
            Shader.PropertyToID("_CortaoPackedMip1"),
            Shader.PropertyToID("_CortaoPackedMip2"),
            Shader.PropertyToID("_CortaoPackedMip3"),
            Shader.PropertyToID("_CortaoPackedMip4"),
            Shader.PropertyToID("_CortaoPackedMip5"),
            Shader.PropertyToID("_CortaoPackedMip6"),
            Shader.PropertyToID("_CortaoPackedMip7"),
        };
    }
}
