#ifndef SS3D_STATION_FLOOR_TILE_INCLUDED
#define SS3D_STATION_FLOOR_TILE_INCLUDED

// Inputs for StationFloorTile ("SS3D/Station Floor Tile"): square floor tiles
// drawn procedurally, then lit by the station passes in StationFlat.hlsl.
//
// The pattern lives in mesh UV space, like the textures it replaces: UV 0..1 is
// one grid cell, cut into 2 × 2 tiles (squares). Grout lines run on every tile
// edge, half a line on each side of a cell border, so neighbouring cells join
// into full lines. A grout diamond sits on every other crossing (the cell
// centre), and Checkerboard alternates two tile colours.
//
// Floor paint: each square can be painted a palette colour in the map editor.
// The colours come from one global texture with a texel per square, looked up
// by world position (FloorPaintView builds it), so paint costs no per-floor
// property and the squares stay where they are whichever way a floor faces.
//
// Lines are filtered against the pixel footprint (after Ben Golus, "The Best
// Darn Grid Shader (Yet)"): never drawn thinner than a pixel, faded by coverage
// instead, so thin grout neither breaks up nor shimmers at gameplay zoom.

#define STATION_CUSTOM_INPUT
#define STATION_ALBEDO_WS

// Fixed: the squares a cell is cut into, and so the squares floor paint colours.
#define FLOOR_TILES_PER_CELL 2.0

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Read by the shared station passes. _MainTex is not drawn here: UI icon
// previews (IconPreviewGenerator → ObjectIcon) render it with _Color.
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _BumpMap_ST;
    half4 _Color;
    half4 _EmissionColor;
    half _BumpScale;
    half _Cutoff;

    half4 _TileColor;
    half4 _TileColorB;
    half4 _GroutColor;
    half _Checker;
    float _LineWidth;
    float _DiamondSize;
CBUFFER_END

// Coverage (0..1) of grout lines one square apart, _LineWidth wide (in squares).
float FloorTile_Lines(float2 p)
{
    float4 pDDXY = float4(ddx(p), ddy(p));
    float2 pDeriv = float2(length(pDDXY.xz), length(pDDXY.yw));

    float2 targetWidth = _LineWidth;
    float2 drawWidth = clamp(targetWidth, pDeriv, 0.5);
    float2 lineAA = max(pDeriv, 1e-6) * 1.5;

    // 0 on a line, 1 halfway between two lines.
    float2 lineUV = 1.0 - abs(frac(p) * 2.0 - 1.0);
    float2 lines = smoothstep(drawWidth + lineAA, drawWidth - lineAA, lineUV);
    // Thinner than a pixel: keep the pixel-wide line, fade it by its real width.
    lines *= saturate(targetWidth / drawWidth);
    // Tiles a couple of pixels across: settle on the average instead of moiré.
    lines = lerp(lines, targetWidth, saturate(pDeriv * 2.0 - 1.0));

    return lerp(lines.x, 1.0, lines.y);
}

// Coverage (0..1) of grout diamonds on every other crossing (odd, odd squares).
float FloorTile_Diamonds(float2 p)
{
    // Distance to the nearest diamond centre, per axis, in squares (0..1).
    float2 q = 2.0 * abs(frac(p * 0.5) - 0.5);
    float distanceL1 = q.x + q.y;
    float radius = _DiamondSize * 0.5;

    float aa = max(fwidth(distanceL1), 1e-6);
    float diamond = saturate((radius - distanceL1) / aa + 0.5);

    // Far away: fade to the diamond's share of a 2 × 2 block (area 2r² of 4).
    float pDeriv = max(fwidth(p.x), fwidth(p.y));
    return lerp(diamond, radius * radius * 0.5, saturate(pDeriv * 2.0 - 1.0));
}

// Floor paint, outside UnityPerMaterial: globals set by FloorPaintView, shared by
// every floor. Unset globals read zero, which is no paint.
TEXTURE2D(_FloorPaintMap);
SAMPLER(sampler_FloorPaintMap);
// xy: the texture's first texel in world half-tiles; zw: 1 / texture size in texels.
float4 _FloorPaintRect;

// The paint on the square under positionWS: rgb colour, a = 1 when painted.
half4 FloorTile_Paint(float3 positionWS)
{
    // A square is half a tile; tile (x, z) is centred on the integer point (x, z).
    float2 texel = floor((positionWS.xz + 0.5) * 2.0) - _FloorPaintRect.xy;
    float2 uv = (texel + 0.5) * _FloorPaintRect.zw;
    if (_FloorPaintRect.z <= 0.0 || any(uv < 0.0) || any(uv > 1.0))
        return half4(0.0, 0.0, 0.0, 0.0);

    return SAMPLE_TEXTURE2D_LOD(_FloorPaintMap, sampler_FloorPaintMap, uv, 0);
}

half3 FloorTile_Pattern(float2 meshUV, half4 paint)
{
    float2 p = meshUV * FLOOR_TILES_PER_CELL;

    float2 square = floor(p);
    half odd = (half)fmod(abs(square.x + square.y), 2.0);
    half3 tile = lerp(_TileColor.rgb, _TileColorB.rgb, odd * _Checker);
    tile = lerp(tile, paint.rgb, paint.a);

    float grout = max(FloorTile_Lines(p), FloorTile_Diamonds(p));
    return lerp(tile, _GroutColor.rgb, (half)grout);
}

// Unpainted: the depth and shadow passes only reach this through alpha clip.
half4 Station_Albedo(float2 meshUV)
{
    return half4(FloorTile_Pattern(meshUV, half4(0.0, 0.0, 0.0, 0.0)) * _Color.rgb, 1.0);
}

half4 Station_AlbedoWS(float2 meshUV, float3 positionWS)
{
    return half4(FloorTile_Pattern(meshUV, FloorTile_Paint(positionWS)) * _Color.rgb, 1.0);
}

#endif
