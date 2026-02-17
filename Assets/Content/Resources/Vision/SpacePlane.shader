Shader "Custom/SpacePlane"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        ZWrite On 
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        CGPROGRAM
        #include "VisionCG.cginc"
        #pragma surface surf Standard fullforwardshadows alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            if(IsVisible(IN.worldPos)) 
            {
                discard;
            }
            
            float4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgba;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            
        }
        ENDCG
    }
        Fallback "Diffuse"
}
