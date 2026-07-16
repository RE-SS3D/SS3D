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

float3 VisionClipToWorld(float2 posClip)
{
    float2 screenUV = posClip * 0.5 + 0.5;
    float rawDepth = SampleSceneDepth(screenUV);

#if !defined(UNITY_REVERSED_Z)
    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
#endif

    if (VisionDepthIsSky(rawDepth))
        return _PlayerPos.xyz;

    // posClip comes from the fullscreen triangle's texture UV (screenUV * 2 - 1), which is
    // flipped relative to true clip space on APIs where UNITY_UV_STARTS_AT_TOP is set (D3D,
    // Vulkan, Metal). Un-flip before multiplying by the CPU-supplied camera inverse
    // view-projection, otherwise the reconstructed world position is mirrored vertically and
    // the vision mask samples the wrong geometry (walls read as open air and vice versa).
    float2 ndc = posClip;
#if UNITY_UV_STARTS_AT_TOP
    ndc.y = -ndc.y;
#endif

    // In a fullscreen/blit pass the bound VP is the blit matrix, not the camera's,
    // so reconstruct world position from a CPU-supplied camera inverse view-projection.
    float4 adjClip = float4(ndc, rawDepth, 1.0);
    float4 worldSpace = mul(_PlayerCameraInvViewProj, adjClip);
    return worldSpace.xyz / worldSpace.w;
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
    float wallDepth = SAMPLE_TEXTURE2D(_VisionMap, sampler_VisionMap, viewUV).r * _ViewRange;

    return currentDepth < wallDepth;
}

bool VisionIsVisibleClip(float2 posClip)
{
    return VisionIsVisibleWorld(VisionClipToWorld(posClip));
}

bool VisionIsVisibleScreenUV(float2 screenUV)
{
    return VisionIsVisibleClip(screenUV * 2.0 - 1.0);
}

#endif
