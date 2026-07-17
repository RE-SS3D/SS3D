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

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // Same raw UV convention as AtmosVert — do NOT use GetFullScreenTriangleTexCoord
            // here. That helper pre-flips Y on UNITY_UV_STARTS_AT_TOP, which double-flips when
            // paired with Atmos-style depth sampling and causes plenums to read as visible
            // through walls.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return VisionIsVisibleScreenUV(input.uv)
                    ? half4(1.0, 1.0, 1.0, 1.0)
                    : half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
}
