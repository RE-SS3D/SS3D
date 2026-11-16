#ifndef SS3D_ST_INPUT_INCLUDED
#define SS3D_ST_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

TEXTURE2D(_ToonRamp);
SAMPLER(sampler_ToonRamp);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _BumpMap_ST;
    float4 _ToonRamp_ST;
    float4 _Color;
    float _AmbientCol;
    float _ColIntense;
    float _ColBright;

    float _Segmented;
    float _Steps;
    float _StpSmooth;
    float _Offset;

    float _HalfToon;
    float _RampStrength;
    float4 _ShadowTint;
    float _Clipped;
    float _MinLight;
    float _MaxLight;
    float _Lumin;
    float _BumpScale;
    float _SpecIntensity;
    float _SpecPower;
    float4 _EmissionColor;

    float4 _ShnColor;
    float _ShnOverlap;
    float _ShnIntense;
    float _ShnRange;
    float _ShnSmooth;

    float4 _OtlColor;
    float _OtlWidth;
CBUFFER_END

#endif
