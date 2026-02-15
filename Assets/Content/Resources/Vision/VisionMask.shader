Shader "Vision/VisionMask"
{
    //TODO Clean this up for multiplatform and add comments
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "VisionCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex.xy, 0, 1);
                o.uv = v.vertex.xy;

                if (_ProjectionParams.x < 0) o.uv.y = 1 - o.uv.y;

                return o;
            }
            float4 frag(v2f i) : SV_Target 
            {
                if(IsVisible(i.uv)) return float4(1,1,1,1);
                else return float4(0,0,0,0);
            }

            ENDCG
        }
    }
}
