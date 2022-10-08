#ifndef STFUNCTIONS_INCLUDED
#define STFUNCTIONS_INCLUDED

float clamp01 (float value) {
	return clamp(value, 0, 1);
}

float rev (float value) {
	return 1.0 - value;
}

float rev01 (float value) {
	return clamp01(rev(value));
}

float pos (float value) {
	return value > 0 ? 1 : 0;
}

float posz (float value) {
	return value >= 0 ? 1 : 0;
}

float neg (float value) {
	return value < 0 ? 1 : 0;
}

float negz (float value) {
	return value <= 0 ? 1 : 0;
}

float lerp01 (float from, float to, float value) {
	return clamp01(lerp(from, to, value));
}

float invLerp (float from, float to, float value, float equal = 0.) {
	float val = (value - from) / (to - from);
	return from == to ? equal : val;
}

float invLerp01 (float from, float to, float value, float equal = 0.) {
	float val = invLerp(from, to, value, equal);
	return from == to ? val : clamp01(val);
}

float wght_invLerp (float from, float to, float value, bool invert = false) {
	float val = (value - from) / (to - from);

	float wgtMin = !invert ? 0 : 1;
	float wgtMax = !invert ? 1 : 0;
	float wgt = value < from ? wgtMin : wgtMax;

	float res = value == from ? 0.5 : wgt;
	return from == to ? res : val;
}

float smoothstep (float from, float to, float value, float equal) {
	float val = smoothstep(from, to, value);
	return from == to ? equal : val;
}

float wght_smoothstep (float from, float to, float value, bool invert = false) {
	float val = smoothstep(from, to, value);

	float wgtMin = !invert ? 0 : 1;
	float wgtMax = !invert ? 1 : 0;
	float wgt = value < from ? wgtMin : wgtMax;

	float res = value == from ? 0.5 : wgt;
	return from == to ? res : val;
}

float smoothlerp (float from, float to, float value) {
	float val = -(2.0 / ((value + 0.34) * 4.7)) + 1.3;
	return lerp01(from, to, val);
}

float colmagnmin (float3 color) {
	float m1 = min(color.r, color.g);
	return min(m1, color.b);
}

float colmagnmax (float3 color) {
	float m1 = max(color.r, color.g);
	return max(m1, color.b);
}

float colspacemax (float3 color) {
	return rev(colmagnmin(color));
}

float colspacemin (float3 color) {
	return rev(colmagnmax(color));
}

float4 ColorBlend (float4 tcol, float4 dcol, float blendf)
{
	float4 res = tcol;
	res.r = lerp(tcol.r, dcol.r, blendf);
	res.g = lerp(tcol.g, dcol.g, blendf);
	res.b = lerp(tcol.b, dcol.b, blendf);
	res.a = lerp(tcol.a, dcol.a, blendf);

	return res;
}

#endif
