#ifndef SS3D_ST_FORWARD_PASS_INCLUDED
#define SS3D_ST_FORWARD_PASS_INCLUDED

#include "STLighting.hlsl"

struct STAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct STVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 viewDirWS : TEXCOORD2;
    float3 positionWS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

STVaryings ST_Vert(STAttributes input)
{
    STVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.normalWS = normalInputs.normalWS;
    output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
    return output;
}

STSurfaceInput ST_GetSurface(STVaryings input)
{
    STSurfaceInput surface;
    surface.uv = input.uv;
    surface.normalWS = input.normalWS;
    surface.viewDirWS = input.viewDirWS;
    surface.positionWS = input.positionWS;
    return surface;
}

half4 ST_FragLit(STVaryings input, bool swapLightColorBlend, bool gateZeroLight, half alphaMultiplier)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 col = ST_EvaluateLighting(ST_GetSurface(input), swapLightColorBlend, gateZeroLight, alphaMultiplier);
    return col;
}

#endif
