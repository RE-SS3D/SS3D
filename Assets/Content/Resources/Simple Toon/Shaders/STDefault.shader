// Station opaque shader: flat albedo with fixture lighting.
//
//     colour = albedo × (ambient × AO + lights) + emission
//
// Lighting lives in StationFlatLighting.hlsl and is tuned globally through the
// "SS3D/Station Shading" volume override (ambient level, light strength, wrap,
// rolloff, CORTAO occlusion strengths), not per material. Materials reference
// this file by GUID; the file keeps its old Simple Toon name (STDefault) and folder.
//
// Alpha Clip (_ALPHATEST_ON) discards texels below _Cutoff (albedo alpha × colour
// alpha) in every pass, for cutout floor stripes and decals.
Shader "SS3D/Station"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", COLOR) = (1,1,1,1)

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1

        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", COLOR) = (0,0,0,0)

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex StationForwardVertex
            #pragma fragment StationForwardFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            // DBuffer blood / surface marks (body wounds, floor splatters).
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #define STATION_FORWARD_PASS
            #include "StationFlat.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex StationShadowVertex
            #pragma fragment StationShadowFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #define STATION_SHADOW_CASTER_PASS
            #include "StationFlat.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex StationDepthOnlyVertex
            #pragma fragment StationDepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #include "StationFlat.hlsl"
            ENDHLSL
        }

        // Feeds CORTAO (normals), DBuffer decals and Decal Layers (rendering layers).
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex StationDepthNormalsVertex
            #pragma fragment StationDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #define STATION_DEPTH_NORMALS_PASS
            #include "StationFlat.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
