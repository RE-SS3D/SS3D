#ifndef SS3D_VISION_INCLUDED
#define SS3D_VISION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// Written by VisionSubSystem each frame (2026-11_fov-rework D1, D4, D8).
TEXTURE2D(_VisionMap);      // R16: per angular bin, the distance a level ray went before a sight blocker / _ViewRange
TEXTURE2D(_VisionTiles);    // R8: one texel per tile around the player; set where a ray hit a sight blocker
float4 _VisionMap_TexelSize; // z = bin count
float4 _VisionTileOrigin;    // xy = world tile of texel (0,0), z = texture size

float4x4 _PlayerCameraInvViewProj;
float4 _PlayerPos;
float _PlayerAngle;
float _ViewConeWidth;
float _ViewRange;
float _VisionDepthEpsilon;
float _VisionNearBubble;
float _VisionWallMinY;

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

// The bin holding this angle, point-sampled: VisionSubSystem.IsWithinDepths reads the same one.
float VisionWallDepth(float angleFromConeStart)
{
    int count = (int)_VisionMap_TexelSize.z;
    int bin = clamp((int)floor(angleFromConeStart / _ViewConeWidth * count), 0, count - 1);
    return LOAD_TEXTURE2D(_VisionMap, int2(bin, 0)).r * _ViewRange;
}

// Inside a wall or door tile that a ray hit (VisionSubSystem.HitTile and IsSeenOccluderTile).
bool VisionInSeenOccluderTile(float3 posWorld)
{
    int2 tile = (int2)floor(posWorld.xz + 0.5) - (int2)_VisionTileOrigin.xy;
    int size = (int)_VisionTileOrigin.z;
    if (any(tile < 0) || any(tile >= size))
        return false;

    return LOAD_TEXTURE2D(_VisionTiles, tile).r > 0.5;
}

// fov.md §3, 2026-11_fov-rework D1: a point shows when the ray at its angle went at least that far,
// or when it is part of a wall a ray hit (inside that tile, above the floor), so walls show whole.
bool VisionIsVisibleWorld(float3 posWorld)
{
    float2 polarCoords = VisionWorldToFov(posWorld);

    float centerAngle = _ViewConeWidth * 0.5;
    float centerAngleWorld = _PlayerAngle;
    float rawOffset = polarCoords.y - centerAngleWorld;
    float clampedOffset = VisionModulo(rawOffset, TWO_PI) - PI;

    if (abs(clampedOffset) > centerAngle)
        return polarCoords.x <= _VisionNearBubble;

    float currentAngle = centerAngle + clampedOffset;
    if (polarCoords.x <= VisionWallDepth(currentAngle) + _VisionDepthEpsilon)
        return true;

    return posWorld.y > _VisionWallMinY && VisionInSeenOccluderTile(posWorld);
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
