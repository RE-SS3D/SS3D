Shader "SS3D/Combat/Flame"
{
    Properties
    {
        [HideInInspector] _DstBlend ("Destination blend", Float) = 1
        _Intensity ("Intensity", Float) = 1
        _SurfaceFlame ("Surface flame tongue", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _SurfaceFlame;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float ripple = sin(p.x * 13 + p.y * 9 + _Time.y * 17 + input.world.z * 8)
                    * sin(p.y * 16 - _Time.y * 11 + input.world.x * 7);
                float shape = saturate((1 - dot(p, p) + ripple * 0.16) * 1.7);
                if (_SurfaceFlame > 0.5)
                {
                    float height = input.uv.y;
                    float width = lerp(0.95, 0.06, height);
                    float bend = sin(height * 8 + _Time.y * 7 + input.world.x * 5) * height * 0.15;
                    float edge = saturate(1 - abs((p.x + bend) / width));
                    shape = sqrt(edge * saturate(height * 12) * saturate((1 - height) * 5));
                }
                return half4(input.color.rgb * _Intensity, input.color.a * shape * shape);
            }
            ENDHLSL
        }
    }
}
