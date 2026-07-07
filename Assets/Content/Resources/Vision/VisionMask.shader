Shader "Vision/VisionMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "VisionMask"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Vision.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 clipCoords = input.texcoord * 2.0 - 1.0;
                return VisionIsVisibleClip(clipCoords) ? half4(1.0, 1.0, 1.0, 1.0) : half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
}
