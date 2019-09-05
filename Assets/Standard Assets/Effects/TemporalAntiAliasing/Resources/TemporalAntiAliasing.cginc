// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#ifndef __TAA__
#define __TAA__

#pragma only_renderers ps4 xboxone d3d11 d3d9 xbox360 opengl glcore
#pragma exclude_renderers gles

#include "UnityCG.cginc"

#define TAA_USE_GATHER4_FOR_DEPTH_SAMPLE (SHADER_TARGET >= 41)

#define TAA_REMOVE_COLOR_SAMPLE_JITTER 1

#define TAA_USE_EXPERIMENTAL_OPTIMIZATIONS 1

#define TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES 1

#define TAA_COLOR_NEIGHBORHOOD_SAMPLE_PATTERN 2
#define TAA_COLOR_NEIGHBORHOOD_SAMPLE_SPREAD 1.

#if !defined(TAA_DILATE_MOTION_VECTOR_SAMPLE)
    #define TAA_DILATE_MOTION_VECTOR_SAMPLE 1
#endif

#define TAA_CLIP_HISTORY_SAMPLE 1

#define TAA_STORE_FRAGMENT_MOTION_HISTORY 1
#define TAA_FRAGMENT_MOTION_HISTORY_DECAY .95

#define TAA_MINIMUM_MOTION_NUDGE 6.28318530718
#define TAA_MINIMUM_MOTION_SENSITIVITY .05
#define TAA_MAXIMUM_MOTION_NUDGE .5
#define TAA_MAXIMUM_MOTION_SENSITIVITY .055

#define TAA_SHARPEN_OUTPUT 1
#define TAA_FINAL_BLEND_METHOD 2

#if TAA_FINAL_BLEND_METHOD == 0
    #define TAA_FINAL_BLEND_FACTOR _FinalBlendParameters.x
#elif TAA_FINAL_BLEND_METHOD == 2
    #define TAA_FINAL_BLEND_STATIC_FACTOR _FinalBlendParameters.x
    #define TAA_FINAL_BLEND_DYNAMIC_FACTOR _FinalBlendParameters.y
    #define TAA_MOTION_AMPLIFICATION _FinalBlendParameters.z
#endif

struct Input
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float4 uv : TEXCOORD0; // [xy: _MainTex.uv, zw: _HistoryTex.uv]
};

struct Output
{
    float4 first : SV_Target0;
    float4 second : SV_Target1;
};

sampler2D _MainTex;
sampler2D _HistoryTex;

sampler2D _CameraMotionVectorsTexture;

#if TAA_USE_GATHER4_FOR_DEPTH_SAMPLE
    Texture2D _CameraDepthTexture;
    SamplerState sampler_CameraDepthTexture;
#else
    sampler2D _CameraDepthTexture;
#endif

float4 _MainTex_TexelSize;
float4 _HistoryTex_TexelSize;
float4 _CameraDepthTexture_TexelSize;

float2 _Jitter;

#if TAA_SHARPEN_OUTPUT
    float4 _SharpenParameters;
#endif

#if TAA_FINAL_BLEND_METHOD == 2
    float4 _FinalBlendParameters;
#endif

Varyings vertex(Input input)
{
    Varyings output;

    float4 vertex = UnityObjectToClipPos(input.vertex);

    output.vertex = vertex;
    output.uv = input.uv.xyxy;

#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0)
        output.uv.y = 1. - input.uv.y;
#endif

    return output;
}

float4 blit(in Varyings input) : SV_Target
{
    return float4(tex2D(_MainTex, input.uv).rgb, 0.);
}

#if (SHADER_TARGET < 50 && !defined (SHADER_API_PSSL))
    float rcp(float value)
    {
        return 1. / value;
    }
#endif

// Tonemapper from http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
float getMaximumElement(in float3 value)
{
    // AMD_shader_trinary_minmax
    return max(max(value.x, value.y), value.z);
}

float4 map(in float4 color)
{
    return float4(color.rgb * rcp(getMaximumElement(color.rgb) + 1.), color.a);
}

float4 map(in float4 color, in float weight)
{
    return float4(color.rgb * rcp(weight * getMaximumElement(color.rgb) + 1.), color.a);
}

float4 unmap(in float4 color)
{
    return float4(color.rgb * rcp(1. - getMaximumElement(color.rgb)), color.a);
}

float2 getClosestFragment(in float2 uv)
{
    const float2 k = _CameraDepthTexture_TexelSize.xy;

    #if TAA_USE_GATHER4_FOR_DEPTH_SAMPLE
        const float4 neighborhood = _CameraDepthTexture.Gather(sampler_CameraDepthTexture, uv, int2(1, 1));
        float3 result = float3(0., 0., _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv).r);
    #else
        const float4 neighborhood = float4(
            SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv - k),
            SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(k.x, -k.y)),
            SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(-k.x, k.y)),
            SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + k)
        );
        float3 result = float3(0.0, 0.0, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
    #endif

    #if TAA_USE_EXPERIMENTAL_OPTIMIZATIONS
        result = lerp(result, float3(-1., -1., neighborhood.x), step(neighborhood.x, result.z));
        result = lerp(result, float3(1., -1., neighborhood.y), step(neighborhood.y, result.z));
        result = lerp(result, float3(-1., 1., neighborhood.z), step(neighborhood.z, result.z));
        result = lerp(result, float3(1., 1., neighborhood.w), step(neighborhood.w, result.z));
    #else
        if (neighborhood.x < result.z)
            result = float3(-1., -1., neighborhood.x);

        if (neighborhood.y < result.z)
            result = float3(1., -1., neighborhood.y);

        if (neighborhood.z < result.z)
            result = float3(-1., 1., neighborhood.z);

        if (neighborhood.w < result.z)
            result = float3(1., 1., neighborhood.w);
    #endif

    return (uv + result.xy * k);
}

// Adapted from Playdead's TAA implementation
// https://github.com/playdeadgames/temporal
float4 clipToAABB(in float4 color, in float p, in float3 minimum, in float3 maximum)
{
    // note: only clips towards aabb center (but fast!)
    float3 center = .5 * (maximum + minimum);
    float3 extents = .5 * (maximum - minimum);

    // This is actually `distance`, however the keyword is reserved
    float4 offset = color - float4(center, p);
    float3 repeat = abs(offset.xyz / extents);

    repeat.x = max(repeat.x, max(repeat.y, repeat.z));

    if (repeat.x > 1.)
    {
        // `color` is not intersecting (nor inside) the AABB; it's clipped to the closest extent
        return float4(center, p) + offset / repeat.x;
    }
    else
    {
        // `color` is intersecting (or inside) the AABB.

        // Note: for whatever reason moving this return statement from this else into a higher
        // scope makes the NVIDIA drivers go beyond bonkers
        return color;
    }
}

Output fragment(Varyings input)
{
#if TAA_DILATE_MOTION_VECTOR_SAMPLE
    float2 motion = tex2D(_CameraMotionVectorsTexture, getClosestFragment(input.uv.zw)).xy;
#else
    float2 motion = tex2D(_CameraMotionVectorsTexture, input.uv.zw).xy;
#endif

    const float2 k = TAA_COLOR_NEIGHBORHOOD_SAMPLE_SPREAD * _MainTex_TexelSize.xy;
    float2 uv = input.uv.xy;
#if TAA_REMOVE_COLOR_SAMPLE_JITTER && UNITY_UV_STARTS_AT_TOP
    uv -= _MainTex_TexelSize.y < 0 ? _Jitter * float2(1, -1) : _Jitter;
#elif TAA_REMOVE_COLOR_SAMPLE_JITTER
    uv -= _Jitter;
#endif

    float4 color = tex2D(_MainTex, uv);

#if TAA_COLOR_NEIGHBORHOOD_SAMPLE_PATTERN == 0
    // Karis 13: a box filter is not stable under motion, use raw color instead of an averaged one
    float4x4 neighborhood = float4x4(
        tex2D(_MainTex, uv + float2(0., -k.y)),
        tex2D(_MainTex, uv + float2(-k.x, 0.)),
        tex2D(_MainTex, uv + float2(k.x, 0.)),
        tex2D(_MainTex, uv + float2(0., k.y)));

    #if TAA_SHARPEN_OUTPUT
        float4 edges = (neighborhood[0] + neighborhood[1] + neighborhood[2] + neighborhood[3]) * .25;
        color += (color - edges) * _SharpenParameters.x;
        color = max(0, color);
    #endif

    #if TAA_CLIP_HISTORY_SAMPLE
        #if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
            float4 average = map(neighborhood[0], .2) + map(neighborhood[1], .2) + map(neighborhood[2], .2) +
                map(neighborhood[3], .2) + map(color, .2);
        #else
            float4 average = (neighborhood[0] + neighborhood[1] + neighborhood[2] + neighborhood[3] + color) * .2;
        #endif
    #endif

    #if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
        neighborhood[0] = map(neighborhood[0]);
        neighborhood[1] = map(neighborhood[1]);
        neighborhood[2] = map(neighborhood[2]);
        neighborhood[3] = map(neighborhood[3]);

        color = map(color);
    #endif

    float4 minimum = min(min(min(min(neighborhood[0], neighborhood[1]), neighborhood[2]), neighborhood[3]), color);
    float4 maximum = max(max(max(max(neighborhood[0], neighborhood[1]), neighborhood[2]), neighborhood[3]), color);
#elif TAA_COLOR_NEIGHBORHOOD_SAMPLE_PATTERN == 1
    // 0 1 2
    // 3
    float4x4 top = float4x4(
        tex2D(_MainTex, uv + float2(-k.x, -k.y)),
        tex2D(_MainTex, uv + float2(0., -k.y)),
        tex2D(_MainTex, uv + float2(k.x, -k.y)),
        tex2D(_MainTex, uv + float2(-k.x, 0.)));

    //     0
    // 1 2 3
    float4x4 bottom = float4x4(
        tex2D(_MainTex, uv + float2(k.x, 0.)),
        tex2D(_MainTex, uv + float2(-k.x, k.y)),
        tex2D(_MainTex, uv + float2(0., k.y)),
        tex2D(_MainTex, uv + float2(k.x, k.y)));

    #if TAA_SHARPEN_OUTPUT
        float4 corners = (top[0] + top[2] + bottom[1] + bottom[3]) * .25;
        color += (color - corners) * _SharpenParameters.x;
        color = max(0, color);
    #endif

    #if TAA_CLIP_HISTORY_SAMPLE
        #if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
            float4 average = map(top[0], .111111) + map(top[1], .111111) + map(top[2], .111111) +
                map(top[3], .111111) + map(color, .111111) + map(bottom[0], .111111) +
                map(bottom[1], .111111) + map(bottom[2], .111111) + map(bottom[3], .111111);
        #else
            float4 average = (top[0] + top[1] + top[2] + top[3] + bottom[0] + bottom[1] + bottom[2] + bottom[3] + color) * .111111;
        #endif
    #endif

    #if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
        top[0] = map(top[0]);
        top[1] = map(top[1]);
        top[2] = map(top[2]);
        top[3] = map(top[3]);

        color = map(color);

        bottom[0] = map(bottom[0]);
        bottom[1] = map(bottom[1]);
        bottom[2] = map(bottom[2]);
        bottom[3] = map(bottom[3]);
    #endif

    float4 minimum = min(min(min(min(min(min(min(min(top[0], top[1]), top[2]), top[3]), bottom[0]), bottom[1]), bottom[2]), bottom[3]), color);
    float4 maximum = max(max(max(max(max(max(max(max(top[0], top[1]), top[2]), top[3]), bottom[0]), bottom[1]), bottom[2]), bottom[3]), color);
#else
    float4 topLeft = tex2D(_MainTex, uv - k * .5);
    float4 bottomRight = tex2D(_MainTex, uv + k * .5);

    float4 corners = 4. * (topLeft + bottomRight) - 2. * color;

    #if TAA_SHARPEN_OUTPUT
        color += (color - (corners * .166667)) * 2.718282 * _SharpenParameters.x;
        color = max(0, color);
    #endif

    float4 average = (topLeft + bottomRight) * .5;

    #if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
        average = map(average);

        topLeft = map(topLeft);
        bottomRight = map(bottomRight);

        color = map(color);
    #endif

    float4 luma = float4(Luminance(topLeft.rgb), Luminance(bottomRight.rgb), Luminance(average.rgb), Luminance(color.rgb));
#endif

    float4 history = tex2D(_HistoryTex, input.uv.zw - motion);

#if TAA_COLOR_NEIGHBORHOOD_SAMPLE_PATTERN == 2
    float sensitivity = saturate(smoothstep(TAA_MINIMUM_MOTION_SENSITIVITY * _MainTex_TexelSize.z, TAA_MAXIMUM_MOTION_SENSITIVITY * _MainTex_TexelSize.z, history.a));
    float nudge = lerp(TAA_MINIMUM_MOTION_NUDGE, TAA_MAXIMUM_MOTION_NUDGE, sensitivity) * max(abs(luma.z - luma.w), abs(luma.x - luma.y));

    float4 minimum = lerp(bottomRight, topLeft, step(luma.x, luma.y)) - nudge;
    float4 maximum = lerp(topLeft, bottomRight, step(luma.x, luma.y)) + nudge;
#endif

#if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
    history = map(history);
#endif

#if TAA_CLIP_HISTORY_SAMPLE
    history = clipToAABB(history, history.a, minimum.xyz, maximum.xyz);
#else
    history = clamp(history, minimum, maximum);
#endif

#if TAA_STORE_FRAGMENT_MOTION_HISTORY
    color.a = length(motion * TAA_MOTION_AMPLIFICATION);
#endif

#if TAA_FINAL_BLEND_METHOD == 0
    // Constant blend factor, works most of the time & cheap; but isn't as nice as a derivative of Sousa 13
    color = lerp(color, history, TAA_FINAL_BLEND_FACTOR);
#elif TAA_FINAL_BLEND_METHOD == 1
    // Implements the final blend method from Playdead's TAA implementation
    #if TAA_COLOR_NEIGHBORHOOD_SAMPLE_PATTERN < 2
        float2
    #endif

    luma.xy = float2(Luminance(color.rgb), Luminance(history.rgb));

    float weight = 1. - abs(luma.x - luma.y) / max(luma.x, max(luma.y, .2));
    weight = lerp(TAA_FINAL_BLEND_DYNAMIC_FACTOR, TAA_FINAL_BLEND_STATIC_FACTOR, weight * weight);

    color = lerp(color, history, weight);
#elif TAA_FINAL_BLEND_METHOD == 2
    float weight = clamp(lerp(TAA_FINAL_BLEND_STATIC_FACTOR, TAA_FINAL_BLEND_DYNAMIC_FACTOR,
            history.a), TAA_FINAL_BLEND_DYNAMIC_FACTOR, TAA_FINAL_BLEND_STATIC_FACTOR);

    color = lerp(color, history, weight);
#endif

#if TAA_TONEMAP_COLOR_AND_HISTORY_SAMPLES
    color = unmap(color);
#endif

    Output output;

    output.first = color;

#if TAA_STORE_FRAGMENT_MOTION_HISTORY
    color.a *= TAA_FRAGMENT_MOTION_HISTORY_DECAY;
#endif

    output.second = color;

    return output;
}

#endif
