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

float3 CortaoLoadViewNormal(int2 pixelCoords)
{
    float3 normalWS = _CortaoNormalsTexture.Load(int3(pixelCoords, 0)).xyz;
    float3 normalVS = mul((float3x3)_CortaoWorldToView, normalWS);
    float lengthSq = dot(normalVS, normalVS);
    // A depth pixel without a written normal would otherwise give NaN occlusion.
    return lengthSq > 1e-8 ? normalVS * rsqrt(lengthSq) : float3(0.0, 0.0, 1.0);
}

#endif
