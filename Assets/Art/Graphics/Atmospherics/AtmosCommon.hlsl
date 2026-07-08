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
TEXTURE2D(_AtmosFlow);
SAMPLER(sampler_AtmosFlow);
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
float _AtmosGlowStrength;
float _AtmosIgnitionTemperature;
float _AtmosDistortionStrength;
float _AtmosDistortionNoiseScale;
float _AtmosDistortionNoiseSpeed;
float _AtmosTurbulenceStrength;
float _AtmosFlowNoiseScale;
float _AtmosFlowSpeed;
float4 _AtmosFireCoreColor;
float _AtmosFlickerAmount;
float _AtmosFlickerSpeed;
float _AtmosFireHeightBoost;
float _AtmosFireRiseStrength;
float _AtmosFireDistortionBoost;

#define ATMOS_COMPOSITION_GAS_CHANNELS 4
float4 _AtmosGasScatter[ATMOS_COMPOSITION_GAS_CHANNELS];
float4 _AtmosGasEmission[ATMOS_COMPOSITION_GAS_CHANNELS];
float4 _AtmosGasMisc[ATMOS_COMPOSITION_GAS_CHANNELS];

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

float AtmosDistortionHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float AtmosDistortionNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = AtmosDistortionHash(i);
    float b = AtmosDistortionHash(i + float2(1.0, 0.0));
    float c = AtmosDistortionHash(i + float2(0.0, 1.0));
    float d = AtmosDistortionHash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float2 AtmosSampleFlow(float2 worldXZ)
{
    float2 atlasUV = AtmosWorldToAtlasUV(worldXZ);
    if (!AtmosIsAtlasUVValid(atlasUV))
        return 0.0;

    // Flow atlas stores direction in 0..1; unpack to -1..1.
    float2 packed = SAMPLE_TEXTURE2D_LOD(_AtmosFlow, sampler_AtmosFlow, atlasUV, 0).rg;
    return packed * 2.0 - 1.0;
}

// Scroll procedural noise by the flow field so fog and plasma appear to move with gas.
float AtmosFlowModulation(float2 worldXZ, float fire)
{
    if (_AtmosTurbulenceStrength <= 0.0)
        return 1.0;

    float2 flow = AtmosSampleFlow(worldXZ);
    float flowMag = length(flow);
    if (flowMag <= 0.001 && fire <= 0.001)
        return 1.0;

    float2 rise = float2(flow.x, abs(flow.y) + fire * _AtmosFireRiseStrength);
    float2 noiseCoord = worldXZ * _AtmosFlowNoiseScale + rise * _Time.y * _AtmosFlowSpeed;
    float noise = AtmosDistortionNoise(noiseCoord) * 2.0 - 1.0;
    float drive = max(flowMag, fire * 0.5);
    return 1.0 + noise * _AtmosTurbulenceStrength * drive;
}

float AtmosGetLocalVolumeHeight(float2 worldXZ)
{
    float fire = AtmosSampleFire(worldXZ);
    return _AtmosVolumeHeight * (1.0 + fire * _AtmosFireHeightBoost);
}

float AtmosFireFlicker(float2 worldXZ)
{
    if (_AtmosFlickerAmount <= 0.0)
        return 1.0;

    float phase = AtmosDistortionHash(worldXZ) * 6.2831853;
    return 1.0 + _AtmosFlickerAmount * sin(_Time.y * _AtmosFlickerSpeed + phase);
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

// Number density is proportional to P/T. Hot over-pressurized cells can have P >> P_ref
// while n is still near ambient — raw pressure alone makes fog/distortion saturate incorrectly.
float AtmosReferenceNumberDensity()
{
    return _AtmosReferencePressure / 293.15;
}

float AtmosNumberDensityRatio(float pressure, float temperature)
{
    float t = max(temperature, 293.15);
    return (pressure / t) / max(AtmosReferenceNumberDensity(), 1e-3);
}

float AtmosPressureFogDensity(float pressure, float temperature)
{
    float excess = max(0.0, AtmosNumberDensityRatio(pressure, temperature) - 1.0);
    float scale = _AtmosFogPressureScale / max(_AtmosReferencePressure, 1e-3);
    return saturate(excess / max(scale, 1e-3));
}

float AtmosSampleGasDensity(float2 worldXZ, float sampleY)
{
    int mask = AtmosSampleMask(worldXZ);
    if (mask == 0 || mask == 2 || mask == 3)
        return 0.0;

    float2 local = worldXZ - _AtmosAtlasBounds.xy;
    float2 size = max(_AtmosAtlasBounds.zw, float2(1.0, 1.0));
    float2 distToSides = min(local + 0.5, (size - 0.5) - local);
    float edgeMin = min(distToSides.x, distToSides.y);
    float edgeFade = smoothstep(0.0, 1.25, edgeMin);

    float fire = AtmosSampleFire(worldXZ);
    float localHeight = AtmosGetLocalVolumeHeight(worldXZ);
    float heightFalloff = saturate(1.0 - sampleY / max(localHeight, 1e-3));

    float pressure = AtmosSamplePressure(worldXZ);
    float temperature = AtmosSampleTemperature(worldXZ);
    float tileDensity = AtmosPressureFogDensity(pressure, temperature);
    return tileDensity * heightFalloff * edgeFade * AtmosFlowModulation(worldXZ, fire);
}

void AtmosSampleCompositionScatter(
    float2 worldXZ,
    float sampleY,
    out float density,
    out float3 tint)
{
    density = 0.0;
    tint = 0.0;

    int mask = AtmosSampleMask(worldXZ);
    if (mask != 1)
        return;

    float4 composition = AtmosSampleComposition(worldXZ);
    float fire = AtmosSampleFire(worldXZ);
    float localHeight = AtmosGetLocalVolumeHeight(worldXZ);
    float heightNorm = sampleY / max(localHeight, 1e-3);
    float plumeMask = smoothstep(0.12, 0.38, heightNorm) * (1.0 - smoothstep(0.82, 1.0, heightNorm));
    float coreMask = 1.0 - saturate(fire * 2.5);
    float flowMod = AtmosFlowModulation(worldXZ, fire);

    float weightedDensity = 0.0;
    float3 weightedColor = 0.0;

    [unroll]
    for (int i = 0; i < ATMOS_COMPOSITION_GAS_CHANNELS; i++)
    {
        float frac = composition[i];
        float scatterStrength = _AtmosGasScatter[i].a;
        if (frac <= 0.001 || scatterStrength <= 0.001)
            continue;

        float channelWeight = frac * scatterStrength;
        // CO₂ (channel 2) uses rising plume shaping away from the burn core.
        if (i == 2)
            channelWeight *= plumeMask * coreMask;

        weightedDensity += channelWeight;
        weightedColor += _AtmosGasScatter[i].rgb * channelWeight;
    }

    if (weightedDensity <= 0.001)
        return;

    tint = weightedColor / weightedDensity;
    density = weightedDensity * flowMod;
}

// Shared view-ray march bounds through the atmosphere slab, clipped to scene depth.
bool AtmosComputeSlabMarch(
    float2 uvScreen,
    float deviceDepth,
    bool isSky,
    out float3 rayOrigin,
    out float3 rayDir,
    out float tEnter,
    out float tExit,
    out float segmentLength,
    out int steps)
{
    rayOrigin = 0.0;
    rayDir = float3(0.0, 1.0, 0.0);
    tEnter = 0.0;
    tExit = 0.0;
    segmentLength = 0.0;
    steps = 0;

    float tScene = 0.0;
    AtmosComputeScatterRay(uvScreen, deviceDepth, isSky, rayOrigin, rayDir, tScene);

    float2 planeXZ = AtmosComputeWorldXZOnPlane(uvScreen, 0.0);
    float planeFire = AtmosSampleFire(planeXZ);
    float volumeHeight = max(_AtmosVolumeHeight, 1e-3) * (1.0 + planeFire * _AtmosFireHeightBoost);

    if (abs(rayDir.y) < 1e-5)
    {
        if (rayOrigin.y < 0.0 || rayOrigin.y > volumeHeight)
            return false;

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
        return false;

    segmentLength = tExit - tEnter;
    steps = max(_AtmosSlabSteps, 1);
    int dynamicSteps = (int)ceil(segmentLength * 3.0);
    steps = clamp(max(steps, dynamicSteps), 1, 32);
    return true;
}

float3 AtmosSamplePointEmission(float2 worldXZ, float sampleY)
{
    int mask = AtmosSampleMask(worldXZ);
    if (mask != 1)
        return 0.0;

    float4 composition = AtmosSampleComposition(worldXZ);
    float temperature = AtmosSampleTemperature(worldXZ);
    float fire = AtmosSampleFire(worldXZ);
    float tempFactor = saturate((temperature - _AtmosIgnitionTemperature) /
        max(_AtmosIgnitionTemperature, 1e-3));

    if (tempFactor <= 0.001 && fire <= 0.001)
        return 0.0;

    float plasmaFrac = composition[3];
    if (fire <= 0.001 && plasmaFrac <= 0.002)
        return 0.0;

    float localHeight = AtmosGetLocalVolumeHeight(worldXZ);
    float heightFalloff = saturate(1.0 - sampleY / max(localHeight, 1e-3));
    float flicker = AtmosFireFlicker(worldXZ);

    float3 gasEmission = 0.0;
    [unroll]
    for (int i = 0; i < ATMOS_COMPOSITION_GAS_CHANNELS; i++)
    {
        float frac = composition[i];
        float emissionIntensity = _AtmosGasEmission[i].a;
        if (frac <= 0.001 || emissionIntensity <= 0.001)
            continue;

        gasEmission += frac * _AtmosGasEmission[i].rgb * emissionIntensity * tempFactor;
    }

    float3 fireCore = fire * tempFactor * _AtmosFireCoreColor.rgb;
    float3 emission = (fireCore + gasEmission) * flicker * heightFalloff;

    return emission * _AtmosGlowStrength * AtmosFlowModulation(worldXZ, fire);
}

void AtmosEvaluateScatter(
    float2 uvScreen,
    float deviceDepth,
    bool isSky,
    out float gasFog,
    out float compFog,
    out float3 compTint)
{
    gasFog = 0.0;
    compFog = 0.0;
    compTint = 0.0;

    float3 rayOrigin;
    float3 rayDir;
    float tEnter;
    float tExit;
    float segmentLength;
    int steps;
    if (!AtmosComputeSlabMarch(uvScreen, deviceDepth, isSky, rayOrigin, rayDir, tEnter, tExit, segmentLength, steps))
        return;

    float stepLength = segmentLength / steps;
    float gasOpticalDepth = 0.0;
    float compOpticalDepth = 0.0;
    float3 compTintAccum = 0.0;
    float compWeightAccum = 0.0;
    float jitter = frac(sin(dot(uvScreen, float2(12.9898, 78.233))) * 43758.5453);
    float jitterPos = jitter - 0.5;

    UNITY_LOOP
    for (int i = 0; i < steps; i++)
    {
        float t = tEnter + stepLength * (i + 0.5 + jitterPos);
        float3 samplePos = rayOrigin + rayDir * t;
        float2 atlasUV = AtmosWorldToAtlasUV(samplePos.xz);
        if (!AtmosIsAtlasUVValid(atlasUV))
            continue;

        gasOpticalDepth += AtmosSampleGasDensity(samplePos.xz, samplePos.y) * stepLength;

        float sampleDensity;
        float3 sampleTint;
        AtmosSampleCompositionScatter(samplePos.xz, samplePos.y, sampleDensity, sampleTint);
        float sampleContribution = sampleDensity * stepLength;
        compOpticalDepth += sampleContribution;
        compTintAccum += sampleTint * sampleContribution;
        compWeightAccum += sampleContribution;
    }

    gasFog = saturate((1.0 - exp(-gasOpticalDepth)) * _AtmosScatterStrength * _AtmosScatterColor.a);
    compFog = saturate(1.0 - exp(-compOpticalDepth));
    compTint = compTintAccum / max(compWeightAccum, 1e-4);
}

float3 AtmosEvaluateGlow(float2 uvScreen, float deviceDepth, bool isSky)
{
    float3 rayOrigin;
    float3 rayDir;
    float tEnter;
    float tExit;
    float segmentLength;
    int steps;
    if (!AtmosComputeSlabMarch(uvScreen, deviceDepth, isSky, rayOrigin, rayDir, tEnter, tExit, segmentLength, steps))
        return 0.0;

    float stepLength = segmentLength / steps;
    float3 emission = 0.0;
    float jitter = frac(sin(dot(uvScreen, float2(4.898, 7.23))) * 43758.5453);
    float jitterPos = jitter - 0.5;

    UNITY_LOOP
    for (int i = 0; i < steps; i++)
    {
        float t = tEnter + stepLength * (i + 0.5 + jitterPos);
        float3 samplePos = rayOrigin + rayDir * t;
        float2 atlasUV = AtmosWorldToAtlasUV(samplePos.xz);
        if (!AtmosIsAtlasUVValid(atlasUV))
            continue;

        emission += AtmosSamplePointEmission(samplePos.xz, samplePos.y) * stepLength;
    }

    return emission;
}

// Returns a screen-space UV offset driven by tile temperature gradients, gas density, and fire.
float2 AtmosEvaluateDistortionOffset(float2 uvScreen)
{
    float2 worldXZ = AtmosComputeWorldXZOnPlane(uvScreen, 0.0);
    if (AtmosSampleMask(worldXZ) != 1)
        return 0.0;

    float pressure = AtmosSamplePressure(worldXZ);
    float temperature = AtmosSampleTemperature(worldXZ);
    float density = AtmosPressureFogDensity(pressure, temperature);
    if (density <= 0.001 && temperature < 293.15 + 1.0)
        return 0.0;

    float fire = AtmosSampleFire(worldXZ);
    float tempFactor = saturate((temperature - 293.15) / max(_AtmosIgnitionTemperature - 293.15, 1.0));
    if (tempFactor <= 0.001 && fire <= 0.001)
        return 0.0;

    float txp = AtmosSampleTemperature(worldXZ + float2(1.0, 0.0));
    float txm = AtmosSampleTemperature(worldXZ - float2(1.0, 0.0));
    float tzp = AtmosSampleTemperature(worldXZ + float2(0.0, 1.0));
    float tzm = AtmosSampleTemperature(worldXZ - float2(0.0, 1.0));
    float2 grad = float2(txp - txm, tzp - tzm) * 0.5;

    float gradMag = length(grad);
    float gradNorm = gradMag / max(_AtmosIgnitionTemperature, 1.0);
    float noise = AtmosDistortionNoise(worldXZ * _AtmosDistortionNoiseScale + _Time.y * _AtmosDistortionNoiseSpeed);
    float shimmer = (noise * 2.0 - 1.0) * gradNorm;

    float weight = (density + tempFactor * 0.35) * (tempFactor + fire) * (1.0 + fire * _AtmosFireDistortionBoost);
    float2 direction = gradMag > 1e-4 ? grad / gradMag : float2(0.0, 0.0);
    float2 offsetXZ = direction * gradNorm + float2(shimmer, -shimmer) * 0.35;

    return offsetXZ * _AtmosDistortionStrength * weight;
}

// Fullscreen triangle vertex shader shared by scatter, glow, and distortion passes.
struct AtmosAttributes
{
    uint vertexID : SV_VertexID;
};

struct AtmosVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

AtmosVaryings AtmosVert(AtmosAttributes input)
{
    AtmosVaryings output;
    float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
    output.uv = uv;
    output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    return output;
}

#endif
