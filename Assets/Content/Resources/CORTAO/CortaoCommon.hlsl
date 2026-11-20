#ifndef SS3D_CORTAO_COMMON_INCLUDED
#define SS3D_CORTAO_COMMON_INCLUDED

// Unity-side inputs shared by the CORTAO prepass and main pass wrappers.
// Vendored algorithm: Assets/Scripts/External/CORTAO (MIT, Daniel Isheden).

// URP _CameraNormalsTexture from the DepthNormals prepass: world-space xyz in a
// signed format (Simple Toon and URP Lit both write the raw normal in forward).
Texture2D<float4> _CortaoNormalsTexture;

// Unity view matrix (OpenGL convention, camera looks down -Z), which is the view
// space CORTAO expects.
float4x4 _CortaoWorldToView;

// Screen pixels per CORTAO pixel along each axis: 1 at full resolution, 2 when
// CORTAO runs at half resolution. CORTAO pixel p reads the depth and normal of
// screen pixel p * scale (the top-left pixel of its 2x2 block).
uint _CortaoInputScale;

int2 CortaoInputPixel(int2 pixelCoords)
{
    return pixelCoords * int(max(_CortaoInputScale, 1u));
}

float3 CortaoLoadViewNormalAt(int2 screenPixel)
{
    float3 normalWS = _CortaoNormalsTexture.Load(int3(screenPixel, 0)).xyz;
    float3 normalVS = mul((float3x3)_CortaoWorldToView, normalWS);
    float lengthSq = dot(normalVS, normalVS);
    // A depth pixel without a written normal would otherwise give NaN occlusion.
    return lengthSq > 1e-8 ? normalVS * rsqrt(lengthSq) : float3(0.0, 0.0, 1.0);
}

float3 CortaoLoadViewNormal(int2 pixelCoords)
{
    return CortaoLoadViewNormalAt(CortaoInputPixel(pixelCoords));
}

#endif
