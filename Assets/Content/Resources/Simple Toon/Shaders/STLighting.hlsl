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

float4 ST_EvaluateDirectLight(
    STSurfaceInput surface,
    half3 lightColor,
    half3 lightDirectionWS,
    half lightAttenuation,
    bool swapLightColorBlend)
{
    STToonSettings settings = ST_GetToonSettings();

    float3 normal = normalize(surface.normalWS);
    float3 lightDir = normalize(lightDirectionWS);
    float3 viewDir = normalize(surface.viewDirWS);
    float3 halfVec = normalize(lightDir + viewDir);
    float3 forward = ST_GetCameraForwardWS();

    float ndotl = dot(normal, lightDir);
    float ndoth = dot(normal, halfVec);
    float vdotn = dot(viewDir, normal);
    float fdotv = dot(forward, -viewDir);

    float toon = ST_Toon(ndotl, lightAttenuation, settings);

    float4 shadeCol = settings.darkColor;
    float4 litCol = swapLightColorBlend
        ? ST_ColorBlend(_Color, float4(lightColor, 1.0), _AmbientCol)
        : ST_ColorBlend(float4(lightColor, 1.0), _Color, _AmbientCol);
    float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, surface.uv) * litCol * _ColIntense + _ColBright;

    float4 blendCol = ST_ColorBlend(shadeCol, texCol, toon);
    float4 postCol = ST_PostEffects(blendCol, toon, lightAttenuation, ndotl, settings);
    return postCol;
}

float4 ST_EvaluateLighting(
    STSurfaceInput surface,
    bool swapLightColorBlend,
    bool gateZeroLight,
    half alphaMultiplier)
{
    float4 result = 0.0;

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
#else
    Light mainLight = GetMainLight();
#endif

    bool mainLightActive = dot(mainLight.color, 1.0) > 0.0;
    if (!gateZeroLight || mainLightActive)
    {
        result = ST_EvaluateDirectLight(
            surface,
            mainLight.color,
            mainLight.direction,
            mainLight.shadowAttenuation * mainLight.distanceAttenuation,
            swapLightColorBlend);
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint additionalLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < additionalLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, surface.positionWS);
        float4 addCol = ST_EvaluateDirectLight(
            surface,
            light.color,
            light.direction,
            light.shadowAttenuation * light.distanceAttenuation,
            swapLightColorBlend);
        result = max(result, addCol);
    }
#endif

    result.rgb += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, surface.uv).rgb * _EmissionColor.rgb;
    result.a = alphaMultiplier;
    return result;
}

#endif
