#ifndef SS3D_ST_CORE_INCLUDED
#define SS3D_ST_CORE_INCLUDED

#include "STFunctions.hlsl"
#include "STInput.hlsl"

struct STToonSettings
{
    float maxLight;
    float steps;
    float stpSmooth;
    float clipped;
    float maxAtten;
    float4 darkColor;
};

STToonSettings ST_GetToonSettings()
{
    STToonSettings settings;
    settings.maxLight = max(_MinLight, _MaxLight);
    settings.steps = (_Segmented > 0.5) ? _Steps : 1.0;
    settings.stpSmooth = (_Segmented > 0.5) ? _StpSmooth : 1.0;
    settings.clipped = (_Clipped > 0.5) ? 1.0 : 0.0;
    settings.maxAtten = 1.0;
    settings.darkColor = float4(0.0, 0.0, 0.0, 1.0);
    return settings;
}

float ST_Toon(float dot, half atten, STToonSettings settings)
{
    float offset = clamp(_Offset, -1.0, 1.0);
    float delta = settings.maxLight - _MinLight;

    float intsPls = dot + offset;
    float intsMax = 1.0 + offset;
    float intense = ST_Clamp01(intsPls / intsMax);

    float step = 1.0 / floor(settings.steps);
    int litNum = ceil(intense / step);
    float lit = litNum * step;

    float reduceV = _Offset - 1.0;
    float reduceRes = 1.0 - ST_Clamp01(reduceV / 0.1);
    float reduce = litNum == 1 ? reduceRes : 1.0;

    float smthStart = lit - step;
    float smthEnd = smthStart + step * settings.stpSmooth;

    float smthLrp = ST_InvLerp01(smthEnd, smthStart, intense, 0.0);
    float smthStp = ST_SmoothStepRange(smthEnd, smthStart, intense, 0.0);

    float smoothV = ST_SmoothLerp(smthStp, smthLrp, settings.stpSmooth);
    float smooth = ST_Clamp01(lit - smoothV * reduce * step);

    float attenInv = clamp(atten, 1.0 - settings.maxAtten, 1.0);
    float dimLit = smooth * attenInv;
    float dimDlt = dimLit - _MinLight;

    float lumLight = settings.maxLight + _Lumin;
    float lumDlt = lumLight - _MinLight;

    float litdClmp = ST_Clamp01(dimDlt);
    float clipCf = litdClmp / delta;

    float clipUncl = _MinLight + clipCf * lumDlt;
    float clipV = clamp(clipUncl, _MinLight, lumLight);

    float lerpV = lumDlt * dimLit;
    float relateV = _MinLight + lerpV;

    float result = settings.clipped * clipV;
    result += (1.0 - settings.clipped) * relateV;
    return result;
}

void ST_PostShine(inout float4 col, float dot, half atten, STToonSettings settings)
{
    float pos = abs(dot - 1.0);
    float len = _ShnRange * 2.0;

    float smthInv = 1.0 - _ShnSmooth;
    float smthEnd = len * smthInv;

    float shine = ST_PosZ(len - pos);
    float smooth = ST_SmoothStepRange(len, smthEnd, pos, 1.0);
    float overlap = (_ShnOverlap > 0.5) ? 1.0 : 0.0;
    float dim = 1.0 - settings.maxAtten * ST_Rev(atten) * ST_Rev(overlap);

    float blend = _ShnIntense * shine * smooth * dim;
    col = ST_ColorBlend(col, _ShnColor, blend);
}

float4 ST_PostEffects(float4 col, float toon, half atten, float ndotl, STToonSettings settings)
{
    ST_PostShine(col, ndotl, atten, settings);
    return col;
}

#endif
