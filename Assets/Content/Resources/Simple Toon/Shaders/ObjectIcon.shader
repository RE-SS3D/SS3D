Shader "Unlit/ObjectIcon"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", COLOR) = (1,1,1,1)
        _ColIntense ("Intensity", Range(0,3)) = 1
        _ColBright ("Brightness", Range(-1,1)) = 0
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _ColIntense;
                float _ColBright;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 lightVector = float3(-0.258, 0.22, 0.966);
                float ndotl = dot(normalize(input.normalWS), lightVector);
                float light = smoothstep(0.0, 0.75, ndotl) * 0.525 + 0.475;
                float4 shadow = float4(0.0, 0.0, 0.04, 0.0) * (1.0 - light);

                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
                half4 texColor = texSample * _Color * _ColIntense + _ColBright;
                texColor.a = texSample.a * _Color.a;
                half4 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.texcoord) * _EmissionColor;

                half3 color = texColor.rgb * light + shadow.rgb + emission.rgb;
                return half4(color, texColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
