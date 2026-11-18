#ifndef SS3D_STATION_FLAT_INCLUDED
#define SS3D_STATION_FLAT_INCLUDED

// Inputs and passes for STDefault ("Simple Toon/SToon Default"), the station's
// opaque shader: flat albedo with fixture lighting (StationFlatLighting.hlsl).
// Property names match the old Simple Toon set so existing materials keep their
// texture, colour, normal map and emission. Every pass includes this file, so
// all passes share one UnityPerMaterial layout (SRP Batcher).

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _BumpMap_ST;
    half4 _Color;
    half4 _EmissionColor;
    half _BumpScale;
CBUFFER_END

struct StationAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ---------------------------------------------------------------------------
// ForwardLit

struct StationVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

StationVaryings StationForwardVertex(StationAttributes input)
{
    StationVaryings output = (StationVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    return output;
}

#if defined(STATION_FORWARD_PASS)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "StationFlatLighting.hlsl"

// Tangent frame from screen-space derivatives: many station meshes (floor tiles)
// ship without usable tangents. Clamped so palette meshes whose UVs are all one
// swatch (zero UV derivatives) keep the geometric normal instead of NaN.
float3 Station_NormalWS(float3 normalWS, float3 positionWS, float2 uv)
{
    normalWS = normalize(normalWS);
    if (_BumpScale <= 0.001)
    {
        return normalWS;
    }

    float2 bumpUV = TRANSFORM_TEX(uv, _BumpMap);
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, bumpUV), _BumpScale);

    float3 dp1 = ddx(positionWS);
    float3 dp2 = ddy(positionWS);
    float2 duv1 = ddx(bumpUV);
    float2 duv2 = ddy(bumpUV);
    float3 dp2perp = cross(dp2, normalWS);
    float3 dp1perp = cross(normalWS, dp1);
    float3 tangentWS = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 bitangentWS = dp2perp * duv1.y + dp1perp * duv2.y;
    float invMax = rsqrt(max(max(dot(tangentWS, tangentWS), dot(bitangentWS, bitangentWS)), 1e-8));
    float3x3 tangentToWorld = float3x3(tangentWS * invMax, bitangentWS * invMax, normalWS);
    return normalize(TransformTangentToWorld(normalTS, tangentToWorld));
}

half4 StationForwardFragment(StationVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
    half3 albedo = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color).rgb;

    // DBuffer blood / surface marks tint the albedo, so they are lit like the surface.
#if defined(_DBUFFER)
    ApplyDecalToBaseColor(input.positionCS, albedo);
#endif

    StationSurface surface;
    surface.albedo = albedo;
    surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
    surface.normalWS = Station_NormalWS(input.normalWS, input.positionWS, input.uv);
    surface.positionWS = input.positionWS;
    surface.viewDirWS = GetWorldSpaceViewDir(input.positionWS);
    surface.positionCS = input.positionCS;
    surface.receiveOcclusion = true;

    return half4(Station_Shade(surface), 1.0);
}

#endif

// ---------------------------------------------------------------------------
// DepthOnly

struct StationDepthVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

StationDepthVaryings StationDepthOnlyVertex(StationAttributes input)
{
    StationDepthVaryings output = (StationDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half StationDepthOnlyFragment(StationDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return input.positionCS.z;
}

// ---------------------------------------------------------------------------
// DepthNormals: geometric world normal (CORTAO input) + rendering layers for
// URP Decal Layers (characters vs tiles).

struct StationDepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

StationDepthNormalsVaryings StationDepthNormalsVertex(StationAttributes input)
{
    StationDepthNormalsVaryings output = (StationDepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

#if defined(STATION_DEPTH_NORMALS_PASS)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

void StationDepthNormalsFragment(
    StationDepthNormalsVaryings input,
    out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif

#endif
