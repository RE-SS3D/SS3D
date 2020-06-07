// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Unlit/FogShader"
{
	Properties
	{
		_MainTex ("Mask Texture", 2D) = "white" {}
		_Noise("Top Noise", 2D) = "white" {}
		_Distortion("Distortion", 2D) = "white" {}

		_Color ("Color", Color) = (0., 0. ,0., 1)
		_NoiseStrength("Top Noise Strength", Range(0,.5)) = .05
		_NumberOfStacks("Stacks", Int) = 16
		_Height("Height", float) = 1
		_Speed ("Speed", float) = .1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent+100"}
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		LOD 100
		Pass
		{
			ZWrite On
			ColorMask 0
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2g
			{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					float3 normal : NORMAL;
			};

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : TEXCOORD1;
			
			};

			float4 _Color;
			float _Speed;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _Distortion;
			sampler2D _Noise;
			float _NoiseStrength;
			int _NumberOfStacks;
			float _Height;

			v2g vert (appdata v)
			{
				v2g o;
				o.vertex =v.vertex;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = v.normal;

				return o;
			}

			[maxvertexcount(101)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> tristream) {
				// here goes the real logic.
				g2f o;
				
				o.uv = input[0].uv;
				o.vertex = UnityObjectToClipPos(input[0].vertex);
				o.color = float4(1., 1., 1., 1.);
				tristream.Append(o);

				o.uv = input[1].uv;
				o.vertex = UnityObjectToClipPos(input[1].vertex);
				o.color = float4(1., 1., 1., 1.);
				tristream.Append(o);

				o.uv = input[2].uv;
				o.vertex = UnityObjectToClipPos(input[2].vertex);
				o.color = float4(1., 1., 1., 1.);
				tristream.Append(o);

				tristream.RestartStrip();

				float3 normal = cross(input[1].vertex - input[0].vertex, input[2].vertex - input[0].vertex);

				float offset = _Height / _NumberOfStacks;
					for (float i = 1; i <= _NumberOfStacks; i++) {
						float stack = (1.- i/_NumberOfStacks);
						o.uv = input[0].uv;
						o.vertex = UnityObjectToClipPos( input[0].vertex + normal * offset*i);
						o.color = float4(stack, stack, stack, stack);
						tristream.Append(o);

						o.uv = input[1].uv;
						o.color = float4(stack, stack, stack, stack);
						o.vertex = UnityObjectToClipPos(input[1].vertex + normal * offset*i);
						tristream.Append(o);

						o.uv = input[2].uv;
						o.color = float4(stack, stack, stack, stack);
						o.vertex = UnityObjectToClipPos(input[2].vertex + normal * offset*i);
						tristream.Append(o);

						tristream.RestartStrip();
				}
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
				// sample the texture			
				// float2 dis = tex2D(_Distortion,i.uv * 0.5 + _Time.xx*_Speed);
				float2 dis = float2(0,0);
				// float displacementStrengh = 0.22* (((sin(_Time.y) + sin(_Time.y*0.5 + 1.051))/4.0) +0.5f);
				// dis = dis * displacementStrengh*(1.0 - i.color.xx);
				fixed4 col = tex2D(_MainTex, i.uv * 2.0 + dis.xy);
				float3 noise = tex2D(_Noise, i.uv * 1 + dis.xy);
				
				if (step(col.x+noise.x*_NoiseStrength, i.color.x) <= .0)discard;

				col = _Color;
				col.a = (_Color.a * _Color.a * _Color.a * _Color.a) * (1. - i.color.a) + _Color.a / 10;
				if(col.a > 1) col.a = 1;

				return col;
			}
			ENDCG
		}
		
	}
}