// Station floor tiles drawn procedurally: square tiles, thin grout lines and a
// grout diamond on every other crossing, optionally checkered in two colours.
// Pattern: StationFloorTile.hlsl. Lighting, shadows, depth, CORTAO normals and
// DBuffer decals are the shared station passes (StationFlat.hlsl), so floors
// light, occlude and take blood exactly like STDefault.
//
// UV 0..1 is one grid cell (FloorTile.fbx, the airlock half tiles and the floor
// tile item all map it that way). _Color tints the whole result: structural
// damage drives it through a property block, and UI icons read it with _MainTex.
Shader "SS3D/Station Floor Tile"
{
    Properties
    {
        [Header(Tiles)]
        [IntRange] _TilesPerCell ("Tiles Per Cell", Range(1, 8)) = 2
        _TileColor ("Tile Color", Color) = (0.8, 0.8, 0.8, 1)
        [ToggleUI] _Checker ("Checkerboard", Float) = 0
        _TileColorB ("Checker Tile Color", Color) = (0.53, 0.51, 0.51, 1)

        [Header(Grout)]
        _GroutColor ("Grout Color", Color) = (0.46, 0.46, 0.46, 1)
        _LineWidth ("Line Width (of a tile)", Range(0, 0.2)) = 0.025
        _DiamondSize ("Diamond Size (of a tile)", Range(0, 1)) = 0.25

        [Header(Shared)]
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Icon Texture (UI previews only)", 2D) = "white" {}

        // Read by the shared station passes; floors keep them off.
        [HideInInspector] [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        [HideInInspector] _BumpScale ("Normal Scale", Float) = 0
        [HideInInspector] _EmissionMap ("Emission Map", 2D) = "white" {}
        [HideInInspector] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        [HideInInspector] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
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
            // DBuffer blood / surface marks (floor splatters).
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_instancing

            #define STATION_FORWARD_PASS
            #include "StationFloorTile.hlsl"
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

            #define STATION_SHADOW_CASTER_PASS
            #include "StationFloorTile.hlsl"
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

            #include "StationFloorTile.hlsl"
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #define STATION_DEPTH_NORMALS_PASS
            #include "StationFloorTile.hlsl"
            #include "StationFlat.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
