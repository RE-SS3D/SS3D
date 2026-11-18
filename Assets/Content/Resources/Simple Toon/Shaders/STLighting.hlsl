#ifndef SS3D_ST_LIGHTING_INCLUDED
#define SS3D_ST_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "STCore.hlsl"

struct STSurfaceInput
{
    float2 uv;
    float3 normalWS;
    float3 viewDirWS;
    float3 positionWS;
};

float3 ST_GetCameraForwardWS()
{
    return mul((float3x3)UNITY_MATRIX_I_V, float3(0.0, 0.0, 1.0));
}

void ST_BuildInputData(STSurfaceInput surface, float4 positionCS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = surface.positionWS;
    inputData.normalWS = normalize(surface.normalWS);
    inputData.viewDirectionWS = normalize(surface.viewDirWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    inputData.shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
#else
    inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
#endif
}

// Globals (not PerMaterial), published per camera by CortaoAmbientOcclusionFeature.
// CORTAO occlusion; white when AO is off. Not URP's _SCREEN_SPACE_OCCLUSION path:
// URP strips that keyword from builds without its own SSAO feature.
TEXTURE2D(_SS3DOcclusionTexture);
// StationShading volume: x = flat (unlit) albedo, y = fixture-light reduction
// (0 keeps all), z = occlusion on the flat term, w = occlusion on fixture light.
// All-zero (feature absent) means fixture lighting only, no AO.
float4 _SS3DFlatShading;
// StationShading light pools: x = strength (0 = off), y = wrap past the terminator.
float4 _SS3DLightPools;

half ST_SampleOcclusion(float2 normalizedScreenSpaceUV)
{
#if defined(ST_NO_SCREEN_OCCLUSION)
    // Transparent surfaces are not in the depth-normals prepass, so the texture
    // under them holds the occlusion of whatever is behind.
    return 1.0;
#else
    return SAMPLE_TEXTURE2D(_SS3DOcclusionTexture, sampler_LinearClamp, normalizedScreenSpaceUV).r;
#endif
}

float4 ST_EvaluateDirectLight(
    STSurfaceInput surface,
    half3 lightColor,
    half3 lightDirectionWS,
    half lightAttenuation,
    bool swapLightColorBlend)
{
    STToonSettings settings = ST_GetToonSettings();

    if (dot(lightColor, 1.0) <= 0.0 || lightAttenuation <= 0.0)
    {
        return 0.0;
    }

    float3 normal = normalize(surface.normalWS);
    float3 lightDir = normalize(lightDirectionWS);

    float ndotl = dot(normal, lightDir);
    // Geometric half-toon / cel banding only. Shadow + distance attenuation must
    // scale the final contribution — folding atten into the shade/lit blend left
    // cast shadows as a pastel tint of the lit color (still looks fully lit).
    float toon = ST_Toon(ndotl, 1.0, settings);

    // Half-toon is fixture-driven: radiance must multiply albedo. The legacy
    // full-toon blend can replace lightColor with _Color at _AmbientCol = 1,
    // silently discarding fixture intensity and emergency/departmental tint.
    float4 litCol = (_HalfToon > 0.5)
        ? float4(_Color.rgb * lightColor, _Color.a)
        : swapLightColorBlend
        ? ST_ColorBlend(_Color, float4(lightColor, 1.0), _AmbientCol)
        : ST_ColorBlend(float4(lightColor, 1.0), _Color, _AmbientCol);
    float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, surface.uv) * litCol * _ColIntense + _ColBright;

    float4 shadeCol = (_HalfToon > 0.5)
        ? float4(texCol.rgb * _ShadowTint.rgb, texCol.a)
        : settings.darkColor;
    float4 blendCol = ST_ColorBlend(shadeCol, texCol, toon);
    float4 postCol = ST_PostEffects(blendCol, toon, lightAttenuation, ndotl, settings);

    if (_HalfToon > 0.5 && _SpecIntensity > 0.001)
    {
        float3 viewDir = normalize(surface.viewDirWS);
        float3 halfDir = normalize(lightDir + viewDir);
        float spec = pow(saturate(dot(normal, halfDir)), _SpecPower) * _SpecIntensity;
        postCol.rgb += spec * texCol.rgb;
    }

    return postCol * lightAttenuation;
}

void ST_AccumulateLight(
    inout float4 result,
    inout float3 poolRadiance,
    STSurfaceInput surface,
    Light light,
    bool swapLightColorBlend,
    half directOcclusion)
{
    float distanceAttenuation = light.distanceAttenuation;
    // Limit distance hotspots before applying visibility. Compressing the
    // product would brighten partial shadows near the light source.
    distanceAttenuation = distanceAttenuation / (1.0 + 1.75 * distanceAttenuation);
    float lightAttenuation = light.shadowAttenuation * distanceAttenuation * directOcclusion;
    if (dot(light.color, 1.0) <= 0.0 || lightAttenuation <= 0.0)
    {
        return;
    }

    // Light pools: plain wrapped diffuse, no toon banding or shadow tint, so the
    // flat look still brightens near fixtures. Back faces stay unlit: a full
    // omni term would leak unshadowed fill lights through walls.
    float wrap = _SS3DLightPools.y;
    float poolDiffuse = saturate((dot(normalize(surface.normalWS), normalize(light.direction)) + wrap) / (1.0 + wrap));
    poolRadiance += light.color * (lightAttenuation * poolDiffuse);

    float4 lit = ST_EvaluateDirectLight(
        surface,
        light.color,
        light.direction,
        lightAttenuation,
        swapLightColorBlend);

    // Half-toon: additive fixture stacking (two wall lights both reach the floor).
    // Full toon: max() preserves the original Simple Toon single-light read.
    if (_HalfToon > 0.5)
    {
        result += lit;
    }
    else
    {
        result = max(result, lit);
    }
}

// Global (not PerMaterial) — map editor / meta views set via Shader.SetGlobalFloat.
float _SS3DAuthoringFullbright;

float4 ST_EvaluateAuthoringFullbright(STSurfaceInput surface, half alphaMultiplier)
{
    float3 normal = normalize(surface.normalWS);
    float3 viewDir = normalize(surface.viewDirWS);
    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, surface.uv) * _Color * _ColIntense + _ColBright;

    // Soft view wrap so forms still read without depending on scene lights.
    float wrap = saturate(dot(normal, viewDir) * 0.5 + 0.5);
    float4 result = lerp(albedo * 0.62, albedo, wrap);
    result.rgb += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, surface.uv).rgb * _EmissionColor.rgb;
    result.a = alphaMultiplier;
    return result;
}

float4 ST_EvaluateLighting(
    STSurfaceInput surface,
    float4 positionCS,
    bool swapLightColorBlend,
    bool gateZeroLight,
    half alphaMultiplier)
{
    if (_SS3DAuthoringFullbright > 0.5)
    {
        return ST_EvaluateAuthoringFullbright(surface, alphaMultiplier);
    }

    float4 result = 0.0;

    InputData inputData;
    ST_BuildInputData(surface, positionCS, inputData);

    half4 shadowMask = CalculateShadowMask(inputData);
    // Neutral: URP only applies aoFactor under _SCREEN_SPACE_OCCLUSION, which ST no
    // longer compiles. CORTAO occlusion is applied below via _SS3DFlatShading.
    AmbientOcclusionFactor aoFactor;
    aoFactor.indirectAmbientOcclusion = 1.0;
    aoFactor.directAmbientOcclusion = 1.0;

    half occlusion = ST_SampleOcclusion(inputData.normalizedScreenSpaceUV);
    half directOcclusion = lerp(1.0, occlusion, _SS3DFlatShading.w);
    float3 poolRadiance = 0.0;

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
#else
    Light mainLight = GetMainLight();
#endif

    bool mainLightActive = dot(mainLight.color, 1.0) > 0.0;
    if (!gateZeroLight || mainLightActive)
    {
        ST_AccumulateLight(result, poolRadiance, surface, mainLight, swapLightColorBlend, directOcclusion);
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        ST_AccumulateLight(result, poolRadiance, surface, light, swapLightColorBlend, directOcclusion);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        ST_AccumulateLight(result, poolRadiance, surface, light, swapLightColorBlend, directOcclusion);
    LIGHT_LOOP_END
#endif

    // Station look: albedo × (flat level × occlusion + soft light pools), under a
    // reduced share of the half-toon fixture light.
    result.rgb *= 1.0 - _SS3DFlatShading.y;
    if (_SS3DFlatShading.x > 0.0 || _SS3DLightPools.x > 0.0)
    {
        float3 albedo = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, surface.uv) * _Color * _ColIntense + _ColBright).rgb;
        half flatOcclusion = lerp(1.0, occlusion, _SS3DFlatShading.z);
        float3 flatLight = _SS3DFlatShading.x * flatOcclusion + _SS3DLightPools.x * poolRadiance;
        result.rgb += albedo * flatLight;
    }

    result.rgb += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, surface.uv).rgb * _EmissionColor.rgb;
    result.a = alphaMultiplier;
    return result;
}

#endif
