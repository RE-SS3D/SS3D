#ifndef SS3D_ST_DEPTH_NORMALS_PASS_INCLUDED
#define SS3D_ST_DEPTH_NORMALS_PASS_INCLUDED

// DepthNormals (+ optional rendering-layer MRT) for Simple Toon opaques.
// Required so URP Decal Layers can tell characters (Default) from tiles (ReceiveWorldDecals).

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct STDepthNormalsAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct STDepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

STDepthNormalsVaryings STDepthNormalsVertex(STDepthNormalsAttributes input)
{
    STDepthNormalsVaryings output = (STDepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
    return output;
}

void STDepthNormalsFragment(
    STDepthNormalsVaryings input,
    out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    outNormalWS = half4(normalWS, 0.0);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
