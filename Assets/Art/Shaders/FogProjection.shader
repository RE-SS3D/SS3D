Shader "Custom/FogProjection"
{
	Properties
	{
		_CurrTexture ("Current Texture", 2D) = "white" {}
		_Color ("Color", Color) = (0, 0, 0, 0)
	}
	SubShader
	{
		Tags { "Queue"="Transparent+100" } // to cover other transparent non-z-write things

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Equal

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};


			float4x4 unity_Projector;
			sampler2D _CurrTexture;
			fixed4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = mul(unity_Projector, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed a = tex2Dproj(_CurrTexture, i.uv).a;

				// weird things happen to minimap if alpha value gets negative
				_Color.a = max(0, _Color.a - a);
				return _Color;
			}
			ENDCG
		}
	}
}