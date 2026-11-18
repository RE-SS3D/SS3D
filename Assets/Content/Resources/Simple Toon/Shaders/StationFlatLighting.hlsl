#ifndef SS3D_STATION_FLAT_LIGHTING_INCLUDED
#define SS3D_STATION_FLAT_LIGHTING_INCLUDED

// Station look: flat (unlit) albedo with fixture lighting on top.
//
//     colour = albedo × (ambient × AO + lights) + emission
//
// "ambient" is a flat, direction-free level, so every surface reads as its
// authored colour. "lights" is the sum of URP lights (main + Forward+ additional)
// with soft wrapped diffuse and URP shadows, then rolled off so fixtures add
// pools of light without blowing out. No toon bands, ramps or shadow tints.
//
// Material-independent: callers pass the surface in, so any shader's CBUFFER
// works (STDefault, STTransparent).

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Globals published per camera by CortaoAmbientOcclusionFeature (StationShading volume).
// CORTAO occlusion; white when AO is off.
TEXTURE2D(_SS3DOcclusionTexture);
// x = ambient level, y = AO on ambient, z = AO on lights, w = 1 once published.
float4 _SS3DStationAmbient;
// x = light strength, y = wrap past the terminator, z = rolloff (0 = linear), w = contrast exponent (>= 1).
float4 _SS3DStationLights;
// x = falloff blend: 0 = URP inverse-square, 1 = range-relative (Built-in pipeline style). yzw unused.
float4 _SS3DStationFalloff;
// Map editor / authoring views (Shader.SetGlobalFloat from MapEditorLighting).
float _SS3DAuthoringFullbright;

struct StationSurface
{
    half3 albedo;
    half3 emission;
    float3 normalWS;
    float3 positionWS;
    float3 viewDirWS;
    float4 positionCS;
    // False for transparents: they are not in the depth-normals prepass, so the
    // occlusion texture under them belongs to whatever is behind.
    bool receiveOcclusion;
};

struct StationLightingParams
{
    half ambient;
    half ambientOcclusion;
    half lightOcclusion;
    half lightStrength;
    half lightWrap;
    half lightRolloff;
    half lightContrast;
    half lightFalloff;
};

StationLightingParams Station_GetLightingParams()
{
    StationLightingParams p;
    if (_SS3DStationAmbient.w > 0.5)
    {
        p.ambient = _SS3DStationAmbient.x;
        p.ambientOcclusion = _SS3DStationAmbient.y;
        p.lightOcclusion = _SS3DStationAmbient.z;
        p.lightStrength = _SS3DStationLights.x;
        p.lightWrap = _SS3DStationLights.y;
        p.lightRolloff = _SS3DStationLights.z;
        p.lightContrast = max(_SS3DStationLights.w, 1.0);
        p.lightFalloff = saturate(_SS3DStationFalloff.x);
    }
    else
    {
        // Nothing published (a renderer without the feature, material previews):
        // match StationShading's defaults instead of rendering black.
        p.ambient = 0.0;
        p.ambientOcclusion = 1.0;
        p.lightOcclusion = 0.4;
        p.lightStrength = 4.0;
        p.lightWrap = 1.0;
        p.lightRolloff = 1.0;
        p.lightContrast = 2.5;
        p.lightFalloff = 1.0;
    }

    return p;
}

// Distance × spot-cone attenuation for additional light i, with the distance
// part blended between URP's physical 1/d² and the Built-in pipeline's
// range-relative 1 / (1 + 25 (d/range)²). Range-relative light keeps its
// strength across most of its range, so fixtures fill rooms with broad pools
// and the gaps between them go dark (old SS3D); 1/d² concentrates everything
// in a hot spot at the fixture. Reads the same per-light data as URP's
// GetAdditionalPerObjectLight; shadows and colour still come from URP's Light.
float Station_AdditionalLightAttenuation(uint i, float3 positionWS, half falloffBlend, out half3 lightDirection)
{
#if USE_CLUSTER_LIGHT_LOOP
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 lightPositionWS = _AdditionalLightsBuffer[lightIndex].position;
    half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[lightIndex].attenuation;
    half4 spotDirection = _AdditionalLightsBuffer[lightIndex].spotDirection;
#else
    float4 lightPositionWS = _AdditionalLightsPosition[lightIndex];
    half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[lightIndex];
    half4 spotDirection = _AdditionalLightsSpotDir[lightIndex];
#endif

    // Directional lights: w = 0 and attenuation.x = 0, so both falloffs are 1.
    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
    lightDirection = half3(lightVector * rsqrt(distanceSqr));

    // attenuation.x = 1 / range², so this is (d / range)².
    float rangeFraction = distanceSqr * float(distanceAndSpotAttenuation.x);
    float window = saturate(1.0 - rangeFraction * rangeFraction);
    window *= window;

    float physical = rcp(distanceSqr);
    float rangeRelative = rcp(1.0 + 25.0 * rangeFraction);
    float distanceAttenuation = lerp(physical, rangeRelative, falloffBlend) * window;

    return distanceAttenuation * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);
}

// Wrapped diffuse: soft, but zero for light arriving from straight behind the surface.
half Station_WrappedDiffuse(float3 normalWS, half3 lightDirection, half wrap)
{
    return saturate((dot(normalWS, lightDirection) + wrap) / (1.0 + wrap));
}

half3 Station_LightRadiance(Light light, float attenuation, float3 normalWS, half wrap)
{
    half diffuse = Station_WrappedDiffuse(normalWS, light.direction, wrap);
    return light.color * (attenuation * light.shadowAttenuation * diffuse);
}

// One additional light, by the loop's index (cluster or per-object). Forward+ walks every
// light whose range reaches the pixel's cluster, most of which miss it: outside a spot's
// cone, past the range window, or arriving from behind. Those return before URP's
// GetAdditionalLight, which samples (and filters) the light's shadow map.
half3 Station_AdditionalLightRadiance(uint lightIndex, InputData inputData, half4 shadowMask,
    AmbientOcclusionFactor aoFactor, float3 normalWS, StationLightingParams p)
{
    half3 lightDirection;
    float attenuation = Station_AdditionalLightAttenuation(lightIndex, inputData.positionWS, p.lightFalloff, lightDirection);
    float unshadowed = attenuation * Station_WrappedDiffuse(normalWS, lightDirection, p.lightWrap);

    half3 radiance = 0.0;
    UNITY_BRANCH
    if (unshadowed > 0.0)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        radiance = light.color * (unshadowed * light.shadowAttenuation);
    }
    return radiance;
}

half3 Station_Shade(StationSurface surface)
{
    float3 normalWS = normalize(surface.normalWS);

    if (_SS3DAuthoringFullbright > 0.5)
    {
        // Authoring: readable forms without depending on scene lights.
        half wrap = saturate(dot(normalWS, normalize(surface.viewDirWS)) * 0.5 + 0.5);
        return surface.albedo * lerp(0.62, 1.0, wrap) + surface.emission;
    }

    StationLightingParams p = Station_GetLightingParams();

    InputData inputData = (InputData)0;
    inputData.positionWS = surface.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = normalize(surface.viewDirWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(surface.positionCS);
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    inputData.shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
#endif

    half occlusion = surface.receiveOcclusion
        ? SAMPLE_TEXTURE2D(_SS3DOcclusionTexture, sampler_LinearClamp, inputData.normalizedScreenSpaceUV).r
        : 1.0;

    // URP only reads aoFactor under _SCREEN_SPACE_OCCLUSION, which this shader
    // never compiles (URP strips it from builds without its own SSAO feature).
    AmbientOcclusionFactor aoFactor;
    aoFactor.indirectAmbientOcclusion = 1.0;
    aoFactor.directAmbientOcclusion = 1.0;
    half4 shadowMask = CalculateShadowMask(inputData);

    half3 radiance = 0.0;

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    radiance += Station_LightRadiance(mainLight, mainLight.distanceAttenuation, normalWS, p.lightWrap);

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        radiance += Station_AdditionalLightRadiance(lightIndex, inputData, shadowMask, aoFactor, normalWS, p);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        radiance += Station_AdditionalLightRadiance(lightIndex, inputData, shadowMask, aoFactor, normalWS, p);
    LIGHT_LOOP_END
#endif

    half3 lights = radiance * p.lightStrength * lerp(1.0, occlusion, p.lightOcclusion);
    // Contrast: light^γ keeps a lit pool (~1) where it is but sinks dim light
    // (fill spill into shadows, falloff between fixtures) toward black.
    lights = pow(max(lights, 0.0), p.lightContrast);
    // Rolloff: x / (1 + k·x) keeps dim light linear and levels off at 1/k; at k = 1
    // fully lit surfaces flatten to their albedo (the "unlit + lighting" look).
    lights = lights / (1.0 + p.lightRolloff * lights);

    half ambient = p.ambient * lerp(1.0, occlusion, p.ambientOcclusion);
    return surface.albedo * (ambient + lights) + surface.emission;
}

#endif
