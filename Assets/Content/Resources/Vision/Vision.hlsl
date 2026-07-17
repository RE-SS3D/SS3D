#ifndef SS3D_VISION_INCLUDED
#define SS3D_VISION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_VisionMap);
SAMPLER(sampler_VisionMap);

float4x4 _PlayerCameraInvViewProj;
float4 _PlayerPos;
float _PlayerAngle;
float _ViewConeWidth;
float _ViewRange;

float VisionModulo(float x, float y)
{
    return x - y * floor(x / y);
}

bool VisionDepthIsSky(float rawDepth)
{
#if defined(UNITY_REVERSED_Z)
    return rawDepth <= 0.0;
#else
    return rawDepth >= 1.0;
#endif
}

float2 VisionWorldToFov(float3 posWorld)
{
    float2 posFov = posWorld.xz - _PlayerPos.xz;
    float rawAngle = (posFov.x == 0.0) ? 0.0 : atan2(posFov.x, -posFov.y);
    float adjAngle = -rawAngle;
    float clampedAngle = VisionModulo(adjAngle, TWO_PI);
    return float2(length(posFov), clampedAngle);
}

// Same contract as AtmosUnproject: `uv` is the raw VisionVert UV (not
// GetFullScreenTriangleTexCoord). Depth is sampled with the UNITY_UV_STARTS_AT_TOP flip;
// NDC Y is flipped for the inverse VP multiply. Mirrors AtmosScatter + AtmosCommon.hlsl —
// the fix that stopped volumetric gas bleeding through walls.
float3 VisionUnproject(float2 uv, float deviceDepth)
{
    float4 positionCS = float4(uv * 2.0 - 1.0, deviceDepth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    positionCS.y = -positionCS.y;
#endif
    float4 positionWS = mul(_PlayerCameraInvViewProj, positionCS);
    return positionWS.xyz / max(positionWS.w, 1e-6);
}

// Prefer this entry from fullscreen passes that use VisionVert (raw 0..1 UV).
float3 VisionWorldFromScreenUV(float2 uvScreen)
{
    float2 depthUV = uvScreen;
#if UNITY_UV_STARTS_AT_TOP
    depthUV.y = 1.0 - depthUV.y;
#endif

    float rawDepth = SampleSceneDepth(depthUV);

#if !defined(UNITY_REVERSED_Z)
    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
#endif

    if (VisionDepthIsSky(rawDepth))
        return _PlayerPos.xyz;

    return VisionUnproject(uvScreen, rawDepth);
}

float3 VisionClipToWorld(float2 posClip)
{
    return VisionWorldFromScreenUV(posClip * 0.5 + 0.5);
}

float4 _VisionMap_TexelSize;

float VisionSampleWallDepth(float2 viewUV)
{
    // Center sample (bilinear via texture filterMode).
    float center = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV).r;

    // Fill small angular holes on continuous surfaces (flat walls) without pulling in a
    // distant corridor ray: only promote neighbors within ~2.5m of the center hit.
    float texel = _VisionMap_TexelSize.x;
    float left = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV + float2(-texel, 0.0)).r;
    float right = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV + float2(texel, 0.0)).r;
    float left2 = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV + float2(-texel * 2.0, 0.0)).r;
    float right2 = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV + float2(texel * 2.0, 0.0)).r;

    float maxNeighbor = max(max(left, right), max(left2, right2));
    float centerWorld = center * _ViewRange;
    float neighborWorld = maxNeighbor * _ViewRange;
    if (abs(neighborWorld - centerWorld) < 2.5)
        center = max(center, maxNeighbor);

    // Extra slack so wall meshes that sit slightly past the collider / polar sample stay lit
    // (especially at glancing angles where XZ depth and ray depth disagree a little).
    return center * _ViewRange + 0.4;
}

bool VisionIsVisibleWorld(float3 posWorld)
{
    float2 polarCoords = VisionWorldToFov(posWorld);

    float centerAngle = _ViewConeWidth * 0.5;
    float centerAngleWorld = _PlayerAngle;
    float rawOffset = polarCoords.y - centerAngleWorld;
    float clampedOffset = VisionModulo(rawOffset, TWO_PI) - PI;

    if (abs(clampedOffset) > centerAngle && polarCoords.x > 1.0)
        return false;

    float currentAngle = centerAngle + clampedOffset;
    float2 viewUV = float2(currentAngle / _ViewConeWidth, 0.0);

    float currentDepth = polarCoords.x;
    float wallDepth = VisionSampleWallDepth(viewUV);

    return currentDepth <= wallDepth;
}

bool VisionIsVisibleScreenUV(float2 screenUV)
{
    return VisionIsVisibleWorld(VisionWorldFromScreenUV(screenUV));
}

bool VisionIsVisibleClip(float2 posClip)
{
    return VisionIsVisibleScreenUV(posClip * 0.5 + 0.5);
}

#endif
