Shader "Hidden/Temporal Anti-aliasing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Perspective
        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vertex
            #pragma fragment fragment
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }

        // Ortho
        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vertex
            #pragma fragment fragment
            #define TAA_DILATE_MOTION_VECTOR_SAMPLE 0
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }

        // First frame History blit
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertex
            #pragma fragment blit
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Perspective
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertex
            #pragma fragment fragment
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }

        // Ortho
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertex
            #pragma fragment fragment
            #define TAA_DILATE_MOTION_VECTOR_SAMPLE 0
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }

        // First frame History blit
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertex
            #pragma fragment blit
            #include "TemporalAntiAliasing.cginc"
            ENDCG
        }
    }
}
