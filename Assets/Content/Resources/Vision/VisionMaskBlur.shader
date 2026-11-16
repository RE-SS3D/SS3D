Shader "Vision/VisionMaskBlur"
{
    // 2026-11_fov-rework D7: the visibility mask is blurred at half resolution and the composite
    // multiplies the scene by mask × blur(mask). Hidden pixels stay fully black (mask 0); visible ones
    // near the edge fade toward it, so the edge softens inward only and nothing hidden lights up.
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_X(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_VisionMaskTex);
        SAMPLER(sampler_VisionMaskTex);
        TEXTURE2D(_VisionBlurTex);
        SAMPLER(sampler_VisionBlurTex);

        // Blur step in UV: x for the horizontal pass, y for the vertical one.
        float4 _VisionBlurStep;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        };

        // Match VisionMask / AtmosVert raw UV; with the sampling flip below, texture to texture is identity.
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
            output.uv = uv;
            output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
            return output;
        }

        float2 SampleUV(float2 uv)
        {
#if UNITY_UV_STARTS_AT_TOP
            uv.y = 1.0 - uv.y;
#endif
            return uv;
        }

        // Nine taps, Gaussian with sigma two taps.
        half Blur(TEXTURE2D_PARAM(source, samplerSource), float2 uv, float2 step)
        {
            const half weights[5] = { 0.2042, 0.1802, 0.1238, 0.0663, 0.0276 };
            half sum = SAMPLE_TEXTURE2D(source, samplerSource, uv).r * weights[0];
            [unroll]
            for (int i = 1; i < 5; i++)
            {
                sum += SAMPLE_TEXTURE2D(source, samplerSource, uv + (step * i)).r * weights[i];
                sum += SAMPLE_TEXTURE2D(source, samplerSource, uv - (step * i)).r * weights[i];
            }

            return sum;
        }
        ENDHLSL

        Pass
        {
            Name "VisionBlurHorizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half blurred = Blur(TEXTURE2D_ARGS(_VisionMaskTex, sampler_VisionMaskTex), SampleUV(input.uv), float2(_VisionBlurStep.x, 0.0));
                return half4(blurred, blurred, blurred, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "VisionBlurVertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half blurred = Blur(TEXTURE2D_ARGS(_VisionBlurTex, sampler_VisionBlurTex), SampleUV(input.uv), float2(0.0, _VisionBlurStep.y));
                return half4(blurred, blurred, blurred, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "VisionComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = SampleUV(input.uv);
                half mask = SAMPLE_TEXTURE2D(_VisionMaskTex, sampler_VisionMaskTex, uv).r;
                if (mask < 0.5)
                    return half4(0.0, 0.0, 0.0, 1.0);

                half soft = saturate(SAMPLE_TEXTURE2D(_VisionBlurTex, sampler_VisionBlurTex, uv).r);
                half4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);
                return half4(color.rgb * soft, color.a);
            }
            ENDHLSL
        }
    }
}
