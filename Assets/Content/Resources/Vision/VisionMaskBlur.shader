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

            // Hard mask, not a soft fog: a tile outside the vision cone tells the player
            // nothing about what's there, so it is fully opaque black with no partial blend.
            half4 Frag(Varyings input) : SV_Target
            {
                float visible = SAMPLE_TEXTURE2D(_FovTex, sampler_FovTex, input.texcoord).r;
                if (visible < 0.5)
                    return half4(0.0, 0.0, 0.0, 1.0);

                return SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, input.texcoord);
            }
            ENDHLSL
        }
    }
}
