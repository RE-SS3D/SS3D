#ifndef SS3D_ATMOS_COMMON_INCLUDED
#define SS3D_ATMOS_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_AtmosPressure);
SAMPLER(sampler_AtmosPressure);
TEXTURE2D(_AtmosTemperature);
SAMPLER(sampler_AtmosTemperature);
TEXTURE2D(_AtmosComposition);
SAMPLER(sampler_AtmosComposition);
TEXTURE2D(_AtmosFire);
SAMPLER(sampler_AtmosFire);
TEXTURE2D(_AtmosMask);
SAMPLER(sampler_AtmosMask);

float4 _AtmosAtlasBounds;
float _AtmosVolumeHeight;
float _AtmosReferencePressure;
float _AtmosFogPressureScale;
float _AtmosScatterStrength;
float4 _AtmosScatterColor;
int _AtmosSlabSteps;
int _AtmosDebugView;
float4x4 _AtmosInvViewProj;

float2 AtmosWorldToAtlasUV(float2 worldXZ)
{
    float2 local = worldXZ - _AtmosAtlasBounds.xy;
    return (local + 0.5) / max(_AtmosAtlasBounds.zw, float2(1.0, 1.0));
}

bool AtmosIsAtlasUVValid(float2 atlasUV)
{
    return all(atlasUV >= 0.0) && all(atlasUV <= 1.0);
}

int AtmosSampleMask(float2 worldXZ)
{
    // Mask is categorical data (empty/simulated/vacuum/blocked), so sample it
    // from a snapped tile center instead of continuous world coordinates.
    // This avoids per-frame camera jitter (TAA) causing edge pixels to flicker
    // between neighboring mask classes.
    float2 local = worldXZ - _AtmosAtlasBounds.xy;
    int2 tile = int2(round(local));
    int2 atlasSize = int2(max(_AtmosAtlasBounds.zw, float2(1.0, 1.0)));
    if (tile.x < 0 || tile.y < 0 || tile.x >= atlasSize.x || tile.y >= atlasSize.y)
        return 0;

    float2 atlasUV = (float2(tile) + 0.5) / float2(atlasSize);
    float encoded = SAMPLE_TEXTURE2D_LOD(_AtmosMask, sampler_AtmosMask, atlasUV, 0).r;
    return (int)round(encoded * 255.0);
}

float AtmosSamplePressure(float2 worldXZ)
{
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    if (!AtmosIsAtlasUVValid(atlasUV))
        return 0.0;

    return SAMPLE_TEXTURE2D_LOD(_AtmosPressure, sampler_AtmosPressure, atlasUV, 0).r;
}

float AtmosSampleTemperature(float2 worldXZ)
{
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    if (!AtmosIsAtlasUVValid(atlasUV))
        return 0.0;

    return SAMPLE_TEXTURE2D_LOD(_AtmosTemperature, sampler_AtmosTemperature, atlasUV, 0).r;
}

float AtmosSampleFire(float2 worldXZ)
{
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    if (!AtmosIsAtlasUVValid(atlasUV))
        return 0.0;

    return SAMPLE_TEXTURE2D_LOD(_AtmosFire, sampler_AtmosFire, atlasUV, 0).r;
}

float4 AtmosSampleComposition(float2 worldXZ)
{
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    if (!AtmosIsAtlasUVValid(atlasUV))
        return 0.0;

    return SAMPLE_TEXTURE2D_LOD(_AtmosComposition, sampler_AtmosComposition, atlasUV, 0);
}

// Renders raw atlas channels for debugging. Returns -1 in alpha when the debug
// view is off so the caller keeps the normal scatter path.
float4 AtmosDebugColor(float3 worldPos)
{
    if (_AtmosDebugView == 0)
        return float4(0, 0, 0, -1);

    float2 worldXZ = worldPos.xz;
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    bool inside = AtmosIsAtlasUVValid(atlasUV);

    // 1: mask (grey=empty, green=simulated, blue=vacuum, red=blocked)
    if (_AtmosDebugView == 1)
    {
        int mask = AtmosSampleMask(worldXZ);
        if (mask == 1) return float4(0, 1, 0, 1);
        if (mask == 2) return float4(0, 0, 1, 1);
        if (mask == 3) return float4(1, 0, 0, 1);
        return float4(0.1, 0.1, 0.1, inside ? 1 : 0);
    }

    // 2: pressure (0..~200 kPa mapped to black..white)
    if (_AtmosDebugView == 2)
    {
        float p = AtmosSamplePressure(worldXZ) / 200.0;
        return float4(p.xxx, inside ? 1 : 0);
    }

    // 3: temperature (173 K blue .. 1000 K red)
    if (_AtmosDebugView == 3)
    {
        float t = saturate((AtmosSampleTemperature(worldXZ) - 173.0) / (1000.0 - 173.0));
        return float4(t, 0.2 * (1 - t), 1 - t, inside ? 1 : 0);
    }

    // 4: fire intensity (black..orange)
    if (_AtmosDebugView == 4)
    {
        float f = saturate(AtmosSampleFire(worldXZ));
        return float4(f, f * 0.4, 0, inside ? 1 : 0);
    }

    // 5: composition (RGB = O2, N2, CO2; plasma tints magenta)
    if (_AtmosDebugView == 5)
    {
        float4 c = AtmosSampleComposition(worldXZ);
        return float4(c.r, c.g, c.b + c.a, inside ? 1 : 0);
    }

    // 6: atlas UV (verifies world->uv mapping)
    return float4(atlasUV, 0, inside ? 1 : 0);
}

float3 AtmosUnproject(float2 uv, float deviceDepth, float4x4 invViewProj)
{
    float4 positionCS = float4(uv * 2.0 - 1.0, deviceDepth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    positionCS.y = -positionCS.y;
#endif
    float4 positionWS = mul(invViewProj, positionCS);
    return positionWS.xyz / max(positionWS.w, 1e-6);
}

// Non-jittered matrix — stable for debug plane projection.
float3 AtmosComputeWorldPosition(float2 uv, float deviceDepth)
{
    return AtmosUnproject(uv, deviceDepth, _AtmosInvViewProj);
}

// Depth-buffer matrix — must match SampleSceneDepth for scatter ray clipping.
float3 AtmosComputeWorldPositionFromDepth(float2 uv, float deviceDepth)
{
    return AtmosUnproject(uv, deviceDepth, UNITY_MATRIX_I_VP);
}

void AtmosComputeViewRay(float2 uv, out float3 rayOrigin, out float3 rayDir)
{
    rayOrigin = _WorldSpaceCameraPos;
    float3 farPos = AtmosComputeWorldPosition(uv, 0.0);
    rayDir = normalize(farPos - rayOrigin);
}

void AtmosComputeScatterRay(float2 uv, float deviceDepth, bool isSky, out float3 rayOrigin, out float3 rayDir, out float tScene)
{
    rayOrigin = _WorldSpaceCameraPos;
    // Explicit defaults so the compiler can’t warn about potentially uninitialized
    // rayDir/tScene in some branches.
    rayDir = float3(0.0, 1.0, 0.0);
    tScene = 0.0;

    if (isSky)
    {
        float3 farPos = AtmosComputeWorldPositionFromDepth(uv, 0.0);
        rayDir = normalize(farPos - rayOrigin);
        tScene = 1e6;
        return;
    }

    float3 surfacePos = AtmosComputeWorldPositionFromDepth(uv, deviceDepth);
    float3 toSurface = surfacePos - rayOrigin;
    float dist = length(toSurface);
    if (dist > 1e-5)
    {
        rayDir = toSurface / dist;
        tScene = dist;
    }
    else
    {
        AtmosComputeViewRay(uv, rayOrigin, rayDir);
        tScene = 0.0;
    }
}

float2 AtmosComputeWorldXZOnPlane(float2 uv, float planeY)
{
    // Build a camera ray from non-jittered clip-space points and intersect it with a
    // constant Y plane so debug masks are stable and do not follow scene geometry depth.
    float3 rayOrigin, rayDir;
    AtmosComputeViewRay(uv, rayOrigin, rayDir);

    float denom = rayDir.y;
    if (abs(denom) < 1e-5)
        return rayOrigin.xz;

    float t = (planeY - rayOrigin.y) / denom;
    return (rayOrigin + rayDir * t).xz;
}

float AtmosSampleGasDensity(float2 worldXZ, float sampleY)
{
    int mask = AtmosSampleMask(worldXZ);
    if (mask == 0 || mask == 2 || mask == 3)
        return 0.0;

    // Fade near atlas borders to avoid a hard rectangular fog "wall".
    float2 local = worldXZ - _AtmosAtlasBounds.xy;
    float2 size = max(_AtmosAtlasBounds.zw, float2(1.0, 1.0));
    float2 distToSides = min(local + 0.5, (size - 0.5) - local);
    // Smooth lateral fade so the fog doesn't end as a hard rectangular boundary.
    float edgeMin = min(distToSides.x, distToSides.y);
    float edgeFade = smoothstep(0.0, 1.25, edgeMin); // fade over ~1 tile

    float pressure = AtmosSamplePressure(worldXZ);
    float excess = max(0.0, pressure - _AtmosReferencePressure);
    float tileDensity = saturate(excess / max(_AtmosFogPressureScale, 1e-3));
    float heightFalloff = saturate(1.0 - sampleY / max(_AtmosVolumeHeight, 1e-3));
    return tileDensity * heightFalloff * edgeFade;
}

float AtmosEvaluateScatter(float2 uvScreen, float deviceDepth, bool isSky)
{
    float3 rayOrigin = 0.0;
    float3 rayDir = float3(0.0, 1.0, 0.0);
    float tScene = 0.0;
    AtmosComputeScatterRay(uvScreen, deviceDepth, isSky, rayOrigin, rayDir, tScene);

    float volumeHeight = max(_AtmosVolumeHeight, 1e-3);
    float tEnter = 0.0;
    float tExit = 0.0;

    if (abs(rayDir.y) < 1e-5)
    {
        if (rayOrigin.y < 0.0 || rayOrigin.y > volumeHeight)
            return 0.0;

        tEnter = 0.0;
        tExit = 1e6;
    }
    else
    {
        float tBottom = (0.0 - rayOrigin.y) / rayDir.y;
        float tTop = (volumeHeight - rayOrigin.y) / rayDir.y;
        tEnter = min(tBottom, tTop);
        tExit = max(tBottom, tTop);
    }

    tEnter = max(tEnter, 0.0);
    tExit = min(tExit, tScene);
    if (tExit <= tEnter)
        return 0.0;

    float segmentLength = tExit - tEnter;
    // Increase steps with ray length to reduce visible layered bands.
    int steps = max(_AtmosSlabSteps, 1);
    int dynamicSteps = (int)ceil(segmentLength * 3.0);
    steps = clamp(max(steps, dynamicSteps), 1, 32);
    float stepLength = segmentLength / steps;
    float opticalDepth = 0.0;
    float jitter = frac(sin(dot(uvScreen, float2(12.9898, 78.233))) * 43758.5453);
    float jitterPos = jitter - 0.5; // [-0.5, 0.5]

    UNITY_LOOP
    for (int i = 0; i < steps; i++)
    {
        float t = tEnter + stepLength * (i + 0.5 + jitterPos);
        float3 samplePos = rayOrigin + rayDir * t;
        float2 atlasUV = AtmosWorldToAtlasUV(samplePos.xz);
        if (!AtmosIsAtlasUVValid(atlasUV))
            continue;

        opticalDepth += AtmosSampleGasDensity(samplePos.xz, samplePos.y) * stepLength;
    }

    // Integrate along the view ray through the atmosphere slab (Beer-Lambert).
    return saturate((1.0 - exp(-opticalDepth)) * _AtmosScatterStrength * _AtmosScatterColor.a);
}

#endif
