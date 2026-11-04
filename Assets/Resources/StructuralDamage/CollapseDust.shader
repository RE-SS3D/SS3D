Shader "SS3D/StructuralDamage/CollapseDust"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float cloud = Noise(input.uv * 5) * 0.65 + Noise(input.uv * 11) * 0.35;
                float edge = 1 - smoothstep(0.15, 0.98, length(p) + (cloud - 0.5) * 0.22);
                half3 ambient = SampleSH(float3(0,1,0));
                Light light = GetMainLight();
                half3 lighting = clamp(ambient + light.color * 0.35, 0.18, 1);
                return half4(input.color.rgb * lighting, input.color.a * edge * lerp(0.4, 1, cloud));
            }
            ENDHLSL
        }
    }
}
