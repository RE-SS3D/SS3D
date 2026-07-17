#ifndef SS3D_VISION_INCLUDED
#define SS3D_VISION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURECUBE(_VisionOcclusionDepth);
SAMPLER(sampler_VisionOcclusionDepth);
float4 _VisionOcclusionZParams;

float4x4 _PlayerCameraInvViewProj;
float4 _PlayerPos;
float _PlayerAngle;
float _ViewConeWidth;

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

// True distance from the player to the nearest occluder in horizontal world direction dir
// (dir.y must be 0, matching the vision cone). Reads the real GPU depth cubemap captured from
// the player's position each frame (see VisionOcclusionCapture.cs) instead of an abstract
// tile-grid map, so occlusion follows actual rendered wall geometry - thin walls, corners,
// doors - rather than a coarse per-tile boolean. Mirrors AtmosCommon.hlsl's
// AtmosIsSampleOccluded fix for the same class of geometry leak.
float VisionOcclusionDistance(float3 dir)
{
    float rawDepth = SAMPLE_TEXTURECUBE(_VisionOcclusionDepth, sampler_VisionOcclusionDepth, dir).r;
    float eyeDepth = LinearEyeDepth(rawDepth, _VisionOcclusionZParams);

    // A cubemap face's dominant axis component is cos(angle-from-face-normal) for a unit
    // direction, which converts the face-local eye depth into a true radial distance.
    float cosOffAxis = max(abs(dir.x), abs(dir.z));
    return eyeDepth / max(cosOffAxis, 1e-4);
}

bool VisionIsVisibleWorld(float3 posWorld)
{
    float2 posFov = posWorld.xz - _PlayerPos.xz;
    float2 polarCoords = VisionWorldToFov(posWorld);

    float centerAngle = _ViewConeWidth * 0.5;
    float centerAngleWorld = _PlayerAngle;
    float rawOffset = polarCoords.y - centerAngleWorld;
    float clampedOffset = VisionModulo(rawOffset, TWO_PI) - PI;

    if (abs(clampedOffset) > centerAngle && polarCoords.x > 1.0)
        return false;

    float currentDepth = polarCoords.x;
    float3 dir = float3(posFov.x, 0.0, posFov.y) / max(currentDepth, 1e-4);
    float wallDepth = VisionOcclusionDistance(dir);

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
