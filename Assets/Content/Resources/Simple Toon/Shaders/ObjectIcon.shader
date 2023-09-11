Shader "Unlit/ObjectIcon" 
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,0)

    }

    SubShader
    {
        Pass
        {
            Tags{ "RenderType" = "Opaque" "Queue" = "Opaque" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _EmissionMap;
            float4 _EmissionColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                fixed4 emission = tex2D(_EmissionMap, i.texcoord) * _EmissionColor;

                float3 lightVector = (float3(-0.258, 0.22, 0.966));

                float NdotL = dot(i.worldNormal, lightVector);
                float light = (smoothstep(0, 0.75, NdotL) * 0.525 + 0.475);
                float4 shadow = float4(0,0,0.04f,0) * (1 - light);


//                float4 rimDot = (1 - dot(viewDir, i.worldNormal));
//                rimDot = (rimDot * rimDot * rimDot) * 0.15;

                fixed3 color = texColor.rgb * light + shadow + emission;
                fixed4 outcolor;
                outcolor.rgb = color.rgb;
                outcolor.a = texColor.a;
                return outcolor;
            }
        ENDCG
        }
    }
}