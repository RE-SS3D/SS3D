#ifndef SS3D_ST_FUNCTIONS_INCLUDED
#define SS3D_ST_FUNCTIONS_INCLUDED

float ST_Clamp01(float value)
{
    return clamp(value, 0.0, 1.0);
}

float ST_Rev(float value)
{
    return 1.0 - value;
}

float ST_Rev01(float value)
{
    return ST_Clamp01(ST_Rev(value));
}

float ST_Pos(float value)
{
    return value > 0.0 ? 1.0 : 0.0;
}

float ST_PosZ(float value)
{
    return value >= 0.0 ? 1.0 : 0.0;
}

float ST_Neg(float value)
{
    return value < 0.0 ? 1.0 : 0.0;
}

float ST_NegZ(float value)
{
    return value <= 0.0 ? 1.0 : 0.0;
}

float ST_Lerp01(float from, float to, float value)
{
    return ST_Clamp01(lerp(from, to, value));
}

float ST_InvLerp(float from, float to, float value, float equal)
{
    float val = (value - from) / (to - from);
    return from == to ? equal : val;
}

float ST_InvLerp01(float from, float to, float value, float equal)
{
    float val = ST_InvLerp(from, to, value, equal);
    return from == to ? val : ST_Clamp01(val);
}

float ST_SmoothStepRange(float from, float to, float value, float equal)
{
    float val = smoothstep(from, to, value);
    return from == to ? equal : val;
}

float ST_SmoothLerp(float from, float to, float value)
{
    float val = -(2.0 / ((value + 0.34) * 4.7)) + 1.3;
    return ST_Lerp01(from, to, val);
}

float4 ST_ColorBlend(float4 targetCol, float4 destCol, float blendf)
{
    float4 res = targetCol;
    res.r = lerp(targetCol.r, destCol.r, blendf);
    res.g = lerp(targetCol.g, destCol.g, blendf);
    res.b = lerp(targetCol.b, destCol.b, blendf);
    res.a = lerp(targetCol.a, destCol.a, blendf);
    return res;
}

#endif
