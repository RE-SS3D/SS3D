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

    float4 worldSpace = mul(_PlayerCameraInvViewProj, float4(posClip, rawDepth, 1.0));

    if (VisionDepthIsSky(rawDepth))
        return _PlayerPos.xyz;

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
