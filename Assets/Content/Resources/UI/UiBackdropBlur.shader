Shader "SS3D/UI/UiBackdropBlur"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        // Pass 0: Dual Kawase downsample
        Pass
        {
            Name "UiBackdropBlurDown"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDown

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            float4 _BlitTexture_TexelSize;
            float _BlurOffset;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            half4 Sample(float2 uv)
            {
#if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
#endif
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            half4 FragDown(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 offset = _BlurOffset * _BlitTexture_TexelSize.xy;

                half4 color = Sample(uv) * 4.0;
                color += Sample(uv + float2(-1.0, -1.0) * offset);
                color += Sample(uv + float2(1.0, -1.0) * offset);
                color += Sample(uv + float2(-1.0, 1.0) * offset);
                color += Sample(uv + float2(1.0, 1.0) * offset);
                return color * 0.125;
            }
            ENDHLSL
        }

        // Pass 1: Dual Kawase upsample
        Pass
        {
            Name "UiBackdropBlurUp"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUp

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            float4 _BlitTexture_TexelSize;
            float _BlurOffset;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            half4 Sample(float2 uv)
            {
#if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
#endif
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            half4 FragUp(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 offset = _BlurOffset * _BlitTexture_TexelSize.xy;

                half4 color = Sample(uv + float2(-1.0, -1.0) * offset);
                color += Sample(uv + float2(1.0, -1.0) * offset);
                color += Sample(uv + float2(-1.0, 1.0) * offset);
                color += Sample(uv + float2(1.0, 1.0) * offset);
                color += Sample(uv + float2(-2.0, 0.0) * offset) * 2.0;
                color += Sample(uv + float2(2.0, 0.0) * offset) * 2.0;
                color += Sample(uv + float2(0.0, -2.0) * offset) * 2.0;
                color += Sample(uv + float2(0.0, 2.0) * offset) * 2.0;
                return color * 0.0833333;
            }
            ENDHLSL
        }
    }
}
