Shader "SS3D/World/Standard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
		
		[Header(Blood)]
		[Space]
		[Toggle(ENABLE_BLOOD)] _Blood ("Enable Blood", Float) = 1
		[ShowProperty(ENABLE_BLOOD)] _BloodTex ("Blood Map", 2D) = "white" {}
		_BloodColor ("Blood Color", Color) = (1,1,1,1)
		_BloodSmoothness ("Blood Smoothness", Range(0,1)) = 0.5
		_Bloodiness ("Bloodiness", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags {
			"RenderType"="Opaque"
			}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		#pragma shader_feature ENABLE_BLOOD

        sampler2D _MainTex;
		sampler2D _BloodTex;

        struct Input
        {
            float2 uv_MainTex;
			float2 uv_BloodTex;
        };

        half _Smoothness;
        //half _Metallic;

		fixed4 _BloodColor;
		half _BloodSmoothness;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
       		fixed4 _Color;
            half _Bloodiness;
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input i, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, i.uv_MainTex) * _Color;
			half s = _Smoothness;

			#ifdef ENABLE_BLOOD
				float val = tex2D(_BloodTex,i.uv_BloodTex).rgb - _Bloodiness + 0.05;
				val = 1-(clamp(val,0,0.05) * 20);

				c.rgb = lerp(c.rgb, _BloodColor, val);
				s = lerp(_Smoothness, _BloodSmoothness, val);
			#endif
			
            o.Albedo = c.rgb;
			o.Smoothness = s;
            o.Metallic = 0; //metallic doesn't seem to be used in the art style, but we can easily re-enable it
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
