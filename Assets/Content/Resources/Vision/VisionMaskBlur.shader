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

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FovTex);
            SAMPLER(sampler_FovTex);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // Match VisionMask / AtmosVert raw UV so the mask RT lines up with scene color.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            // Hard mask, not a soft fog: unseen areas are fully opaque black.
            half4 Frag(Varyings input) : SV_Target
            {
                // Same texture-space flip as AtmosScatter when sampling camera/blit targets.
                float2 uvSample = input.uv;
#if UNITY_UV_STARTS_AT_TOP
                uvSample.y = 1.0 - uvSample.y;
#endif

                float visible = SAMPLE_TEXTURE2D(_FovTex, sampler_FovTex, uvSample).r;
                if (visible < 0.5)
                    return half4(0.0, 0.0, 0.0, 1.0);

                return SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uvSample);
            }
            ENDHLSL
        }
    }
}
