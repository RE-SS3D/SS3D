#ifndef STCORE_INCLUDED
#define STCORE_INCLUDED

#include "STFunctions.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;

float4 _Color;
float4 _DarkColor;
float _AmbientCol;
float _ColIntense;
float _ColBright;

bool _Segmented;
float _Steps;
float _StpSmooth;
float _Offset;

bool _Clipped;
float _MinLight;
float _MaxLight;
float _Lumin;

float _MaxAtten;

float4 _ShnColor;
bool _ShnOverlap;
float _ShnIntense;
float _ShnRange;
float _ShnSmooth;

float Toon (float dot, fixed atten)
{
	float offset = clamp(_Offset, -1, 1);
	float delta = _MaxLight - _MinLight;

	//intense
	float ints_pls = dot + offset;
	float ints_max = 1.0 + offset;
	float intense = clamp01(ints_pls / ints_max);

	//lit
	float step = 1.0 / floor(_Steps);
	int lit_num = ceil(intense / step);
	float lit = lit_num * step;

	//smooth
	float reduce_v = _Offset - 1.0;
	float reduce_res = 1.0 - clamp01(reduce_v / 0.1);  //!v offset plus
	float reduce = lit_num == 1 ? reduce_res : 1;

	float smth_start = lit - step;
	float smth_end = smth_start + step * _StpSmooth;

	float smth_lrp = invLerp01(smth_end, smth_start, intense);
	float smth_stp = smoothstep(smth_end, smth_start, intense, 0.);

	float smooth_v = smoothlerp(smth_stp, smth_lrp, _StpSmooth);
	float smooth = clamp01(lit - smooth_v * reduce * step);

	//shadow
	float atten_inv = clamp(atten, 1.0 - _MaxAtten, 1);
	float dimLit = smooth * atten_inv;
	float dim_dlt = dimLit - _MinLight;

	//luminocity
	float lumLight = _MaxLight + _Lumin;
	float lum_dlt = lumLight - _MinLight;

	//clipped
	float litd_clmp = clamp01(dim_dlt);
	float clip_cf = litd_clmp / delta;

	float clip_uncl = _MinLight + clip_cf * lum_dlt;
	float clip_v = clamp(clip_uncl, _MinLight, lumLight);

	//relative limits
	float lerp_v = lum_dlt * dimLit;
	float relate_v = _MinLight + lerp_v;

	//result
	float result = _Clipped * clip_v;
	result += !_Clipped * relate_v;

	return result;
}

//post effects

void PostShine (inout float4 col, float dot, float atten)
{
	float pos = abs(dot - 1.0);
	float len = _ShnRange * 2;

	float smth_inv = 1.0 - _ShnSmooth;
	float smth_end = len * smth_inv;

	float shine = posz(len - pos);
	float smooth = smoothstep(len, smth_end, pos, 1.);
	float dim = 1.0 - _MaxAtten * rev(atten) * rev(_ShnOverlap);

	float blend = _ShnIntense * shine * smooth * dim;
	col = ColorBlend(col, _ShnColor, blend);
}

float4 PostEffects (float4 col, float toon, float atten, float NdotL, float NdotH, float VdotN, float FdotV)
{
	PostShine(col, NdotL, atten);

	return col;
}

#endif
