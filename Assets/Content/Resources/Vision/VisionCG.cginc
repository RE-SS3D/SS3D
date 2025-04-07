#ifndef VISION_CG_INCLUDED
#define VISION_CG_INCLUDED


UNITY_DECLARE_TEX2D(_CameraDepthTexture);
UNITY_DECLARE_TEX2D(_VisionMap);
float4x4 _PlayerCameraInvViewProj;
float4 _PlayerPos;
float _PlayerAngle;
float _ViewConeWidth;
float _ViewRange;
//TODO setup for multiplatform

inline bool IsVisible(float2 screenUV);
inline bool IsVisible(float3 posWorld);

inline float2 WorldToFov(float3 posWorld);
inline float4 FragToClip(float4 frag);
inline float3 ClipToWorld(float2 posClip);
inline bool depthIsSky(float depth);
inline float modulo(float x, float y);


//Returns true if the point should be visible to the player
//Prefer posWorld version where possible

//Use with fragment position in any normal fragment shader
inline bool IsVisible(float4 posFrag)
{
    float4 posClip = FragToClip(posFrag);
    return IsVisible(posClip.xy);
}
//Use with UVs in postprocessing shaders
inline bool IsVisible(float2 screenUV)
{
    float3 posWorld = ClipToWorld(screenUV);
    return IsVisible(posWorld);
}
//Checks world coordinates against polar depth texture
//Use with world positions in shaders that supply them
inline bool IsVisible(float3 posWorld)
{
    float2 polarCoords = WorldToFov(posWorld);
    
    float centerAngle = _ViewConeWidth / 2;
    float centerAngleWorld = _PlayerAngle;
    float rawOffset = polarCoords.y - centerAngleWorld;
    float clampedOffset = modulo(rawOffset, UNITY_TWO_PI) - UNITY_PI;

    //return early if angle is outside the view cone
    if ((abs(clampedOffset) > centerAngle) && (polarCoords.x > 1)) return false;
    
    float currentAngle = centerAngle + clampedOffset;
    float2 viewUV = float2(currentAngle / _ViewConeWidth, 0);

    float currentDepth = polarCoords.x;
    float wallDepth = UNITY_SAMPLE_TEX2D(_VisionMap, viewUV).r * _ViewRange;

    return currentDepth < wallDepth;
}

// Inputs fragment position (pixelX, pixelY, depth, perspective)
// Outputs clip position([-1,1],[-1,1], depth, perspective)
inline float4 FragToClip(float4 frag)
{
    frag.xy /= _ScreenParams.xy;
    frag.xy = frag.xy * 2 - 1;
    return frag;
}

//Converts clip position ([-1,1][-1,1]) to world position
inline float3 ClipToWorld(float2 posClip)
{
    float2 screenUV = posClip * 0.5 + 0.5;
    float rawDepth = UNITY_SAMPLE_TEX2D(_CameraDepthTexture, screenUV).r;

    #if !defined(UNITY_REVERSED_Z)
        rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
    #endif

    float4 adjClip = float4(posClip, rawDepth, 1);

    float4 worldspace = mul(_PlayerCameraInvViewProj, adjClip);

    if(depthIsSky(rawDepth))
    {
        //Ensures skybox is always visible
        //Skybox should have dedicated handling in the future
        return _PlayerPos;
    }
    return worldspace.xyz / worldspace.w;

}

//Converts world position to polar coordinates (distance, radians) relative to player
inline float2 WorldToFov(float3 posWorld)
{
    float2 posFov = (posWorld - _PlayerPos).xz;
    float rawAngle;

    if (posFov.x == 0) rawAngle = 0;
    else rawAngle = atan2(posFov.x, -posFov.y);

    float adjAngle = -rawAngle;
    float clampedAngle = modulo(adjAngle, UNITY_TWO_PI);

    float worldDepth = length(posFov);
    float2 polarCoords = float2(worldDepth, clampedAngle);

    return polarCoords;
}

//Detect if skybox, requires special handling
inline bool depthIsSky(float depth)
{
    #if defined(UNITY_REVERSED_Z)
        return (depth <= 0.0);
    #else
        return (depth >= 1.0);
    #endif
}

inline float modulo(float x, float y)
{
    return x - y * floor(x/y);
}


#endif
