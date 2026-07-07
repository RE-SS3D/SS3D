Shader "Vision/VisionMaskBlur"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "VisionMaskBlur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Vision.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FovTex);
            SAMPLER(sampler_FovTex);

            float _FovBlurQuality;
            float _FovBlurDirections;
            float2 _FovBlurSize;

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
                float2 diameter = (_FovBlurSize / _ScreenParams.xy) * 2.0;
                float average = 0.0;
                float3 thisPos = VisionClipToWorld(input.texcoord * 2.0 - 1.0);
                float3 thisDepth = thisPos - _WorldSpaceCameraPos.xyz;
                float thisVisible = SAMPLE_TEXTURE2D(_FovTex, sampler_FovTex, input.texcoord).r > 0.5;
                float totalSamples = 0.0;

                for (float angle = 0.0; angle < TWO_PI; angle += TWO_PI / _FovBlurDirections)
                {
                    for (float sampleIndex = 1.0; sampleIndex <= _FovBlurQuality; sampleIndex++)
                    {
                        float2 offset = float2(cos(angle), sin(angle)) * diameter * (sampleIndex / _FovBlurQuality);
                        float2 sampleUV = input.texcoord + offset;
                        float3 samplePos = VisionClipToWorld(sampleUV * 2.0 - 1.0);
                        float3 sampleDepth = samplePos - _WorldSpaceCameraPos.xyz;

                        if (!thisVisible || length(offset) < 0.006 || length(thisDepth - sampleDepth) < 0.5)
                        {
                            totalSamples += 1.0;
                            if (SAMPLE_TEXTURE2D(_FovTex, sampler_FovTex, sampleUV).r < 0.5)
                                average += 1.0;
                        }
                    }
                }

                average /= max(totalSamples, 1.0);
                half4 col = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, input.texcoord);

                if (!thisVisible)
                    col = lerp(col, half4(0.0, 0.0, 0.0, 1.0), 0.2);

                if (average > 0.0)
                    col = half4(0.0, 0.0, 0.0, 1.0) * average + col * (1.0 - average);

                return col;
            }
            ENDHLSL
        }
    }
}
