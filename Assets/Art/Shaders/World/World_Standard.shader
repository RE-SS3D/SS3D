Shader "SS3D/World/Standard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_NormalTex ("Normal", 2D) = "bump" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
		
		[Header(Blood)]
		[Space]
		[Toggle(ENABLE_BLOOD)] _Blood ("Enable Blood", Float) = 1
		_BloodTex ("Blood Map", 2D) = "white" {}
		_BloodColor ("Blood Color", Color) = (1,1,1,1)
		_BloodSmoothness ("Blood Smoothness", Range(0,1)) = 0.5
		_Bloodiness ("Bloodiness", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags {
			"RenderType"="Opaque"
			"DisableBatching"="True"
			}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0
		#pragma shader_feature ENABLE_BLOOD

        sampler2D _MainTex;
		sampler2D _NormalTex;
		sampler2D _BloodTex;
		float4 _BloodTex_ST;

        struct Input
        {
            float2 uv_MainTex;
			float2 uv_NormalTex;
			float2 uv_BloodTex;
			float3 pos;
			float3 normal;
        };

		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.pos = v.vertex.xyz;
			o.normal = abs(v.normal);
		}

        half _Smoothness;
		half _BloodSmoothness;
        //half _Metallic;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(fixed4,_BloodColor)
       		UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(half, _Bloodiness)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input i, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, i.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			half3 n = UnpackNormal(tex2D (_NormalTex, i.uv_NormalTex));
			half s = _Smoothness;
 
			float3 scale = float3(
				length(unity_ObjectToWorld._m00_m10_m20),
				length(unity_ObjectToWorld._m01_m11_m21),
				length(unity_ObjectToWorld._m02_m12_m22)
				);
 
            float3 uvPos = i.pos.xyz;
            float3 uvBlend = i.normal.xyz;
            uvBlend /= dot(abs(uvBlend), float3(1,1,1));
			
            float3 blend = abs(uvBlend);
			float2 uv;
			
			if (blend.x > max(blend.y, blend.z))
			{
				uv = uvPos.yz * _BloodTex_ST.xy;
			}
			else if (blend.z > blend.y)
			{
				uv = uvPos.xy * _BloodTex_ST.xy;
			}
			else
			{
				uv = uvPos.xz * _BloodTex_ST.xy;
			}
			
			
			#ifdef ENABLE_BLOOD
				float val = tex2D(_BloodTex, uv).rgb - UNITY_ACCESS_INSTANCED_PROP(Props, _Bloodiness) + 0.05;
				val = 1-(clamp(val,0,0.05) * 20);

				c.rgb = lerp(c.rgb, UNITY_ACCESS_INSTANCED_PROP(Props, _BloodColor).rgb, val);
				s = lerp(_Smoothness, _BloodSmoothness, val);
			#endif
			
            o.Albedo = c.rgb;
			o.Normal = n.rgb;
			o.Smoothness = s;
            o.Metallic = 0; //metallic doesn't seem to be used in the art style, but we can easily re-enable it
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
