Shader "SS3D/Steel Wall Damage"
{
    Properties
    {
        _Cracked("Cracked", Float) = 0
        _Particle("Particle", Float) = 0
        _SurfaceMode("Surface mode", Float) = 0
        _SurfaceCenter("Surface center", Vector) = (0,0,0,0)
        _SurfaceExtents("Surface extents", Vector) = (1,1,1,0)
        _Variation("Variation", Float) = 0
        _Wood("Wood", Float) = 0
        _Glass("Glass", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Cracked;
            float _Particle;
            float _SurfaceMode;
            float4 _SurfaceCenter;
            float4 _SurfaceExtents;
            float _Variation;
            float _Wood;
            float _Glass;
            CBUFFER_END

            struct Attributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 local : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.local = input.position.xyz;
                output.normal = input.normal;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float Stroke(float2 p, float2 a, float2 b, float width)
            {
                float2 segment = b - a;
                float t = saturate(dot(p - a, segment) / dot(segment, segment));
                float distanceToLine = length(p - a - t * segment);
                return 1 - smoothstep(width, width + max(fwidth(distanceToLine), 0.002), distanceToLine);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_Particle > 0.5)
                {
                    float2 uv = abs(input.uv * 2 - 1);
                    float opacity = 1 - smoothstep(0.24, 1, length(uv));
                    return half4(input.color.rgb, input.color.a * opacity);
                }

                float3 normal = normalize(input.normal);
                if (_SurfaceMode < 0.5 || _SurfaceMode > 1.5)
                    clip(0.65 - abs(normal.y));
                bool alongX = abs(normal.z) > abs(normal.x);
                float side = alongX ? normal.z : normal.x;
                float2 p = float2((alongX ? input.local.x : input.local.z) * sign(side),
                    input.local.y - 1.22);
                p.x += alongX ? 0.045 : -0.065;
                if (_SurfaceMode > 0.5)
                {
                    float3 q = (input.local - _SurfaceCenter.xyz) / max(_SurfaceExtents.xyz, 0.001);
                    if (_SurfaceMode < 1.5)
                    {
                        // Tabletop only: no repeated marks on legs, underside or narrow side faces.
                        clip(normal.y - 0.65);
                        clip(q.y - 0.55);
                        p = q.xz * 0.52;
                    }
                    else
                    {
                        // Each leaf owns its overlay, so the scar travels with the opening door.
                        p = float2(alongX ? q.x : q.z, q.y) * float2(0.48, 0.58);
                    }
                    if (_SurfaceMode > 2.5)
                    {
                        // Machine side casings: preserve the front display and controls.
                        clip(abs(normal.x) - 0.65);
                        p = q.zy * float2(0.52, 0.58);
                    }
                    p -= float2((_Variation - 0.5) * 0.12, (_Variation - 0.5) * 0.08);
                    p.x *= _Variation > 0.5 ? -1 : 1;
                }


                if (_Glass > 0.5)
                {
                    // A small impact star grows into branching fractures, with no metal dent.
                    float radius = _Cracked > 0.5 ? 1 : 0.4;
                    float2 g = p / radius;
                    float fracture = Stroke(g, float2(0, 0), float2(-0.21, 0.28), 0.003);
                    fracture = max(fracture, Stroke(g, float2(0, 0), float2(0.32, 0.12), 0.003));
                    fracture = max(fracture, Stroke(g, float2(0, 0), float2(0.11, -0.34), 0.004));
                    fracture = max(fracture, Stroke(g, float2(0, 0), float2(-0.29, -0.16), 0.003));
                    fracture = max(fracture, Stroke(g, float2(0, 0), float2(-0.03, 0.4), 0.002));
                    if (_Cracked > 0.5)
                    {
                        fracture = max(fracture, Stroke(g, float2(-0.12, 0.16), float2(-0.3, 0.18), 0.002));
                        fracture = max(fracture, Stroke(g, float2(0.19, 0.07), float2(0.3, 0.3), 0.002));
                        fracture = max(fracture, Stroke(g, float2(0.06, -0.18), float2(0.28, -0.3), 0.002));
                        fracture = max(fracture, Stroke(g, float2(-0.16, -0.09), float2(-0.19, -0.35), 0.002));
                    }
                    float chip = 1 - smoothstep(0.008, 0.025, length(p));
                    float opacity = saturate(fracture * 0.88 + chip * 0.55);
                    clip(opacity - 0.015);
                    return half4(0.73, 0.86, 0.89, opacity);
                }

                if (_Wood > 0.5)
                {
                    // Long gouges and splits run with the tabletop grain, exposing pale fibres.
                    float2 grain = p + float2(sin(p.y * 83) * 0.004, 0);
                    float gouge = Stroke(grain, float2(-0.10, -0.17), float2(-0.075, 0.16), 0.010);
                    gouge = max(gouge, Stroke(grain, float2(0.08, -0.09), float2(0.09, 0.12), 0.006));
                    float fibres = Stroke(grain, float2(-0.10, -0.17), float2(-0.075, 0.16), 0.018);
                    fibres = max(fibres, Stroke(grain, float2(0.08, -0.09), float2(0.09, 0.12), 0.012));
                    float split = 0;
                    float edge = 0;
                    if (_Cracked > 0.5)
                    {
                        float2 a = float2(-0.025, -0.41);
                        float2 b = float2(0.005, -0.06);
                        float2 c = float2(-0.018, 0.13);
                        float2 d = float2(0.008, 0.41);
                        split = max(Stroke(grain, a, b, 0.006), Stroke(grain, b, c, 0.016));
                        split = max(split, Stroke(grain, c, d, 0.008));
                        split = max(split, Stroke(grain, b, float2(0.095, 0.24), 0.004));
                        edge = max(Stroke(grain, a, b, 0.012), Stroke(grain, b, c, 0.025));
                        edge = max(edge, Stroke(grain, c, d, 0.015));
                        edge = max(edge, Stroke(grain, b, float2(0.095, 0.24), 0.009));
                    }
                    float darkWood = max(gouge * 0.65, split * 0.96);
                    float exposed = saturate(max(fibres, edge) - max(gouge, split));
                    float opacity = saturate(darkWood + exposed * 0.85);
                    clip(opacity - 0.015);
                    float3 color = lerp(float3(0.10, 0.055, 0.025), float3(0.66, 0.44, 0.23), exposed);
                    return half4(color, opacity);
                }

                // Compressed paint, then a thin exposed-metal lip around the dent.
                float2 dent = float2(p.x + p.y * 0.24, p.y) / float2(0.24, 0.19);
                float r = length(dent) + 0.065 * sin(dent.x * 13 + dent.y * 8);
                float shade = (1 - smoothstep(0.28, 1.06, r)) * 0.57;
                float lip = smoothstep(0.66, 0.82, r) * (1 - smoothstep(0.84, 1.03, r));
                lip *= smoothstep(-0.02, 0.68, -dent.y - dent.x * 0.45);
                float chips = Stroke(p, float2(-0.19, 0.07), float2(-0.13, 0.11), 0.007)
                    + Stroke(p, float2(0.12, -0.13), float2(0.19, -0.16), 0.005);

                float tear = 0;
                float rim = 0;
                if (_Cracked > 0.5)
                {
                    // Break up long straight segments with smaller bends in the torn lip.
                    float2 fracture = p + float2(sin(p.y * 67), sin(p.x * 78 + p.y * 38)) * 0.008;
                    float2 a = float2(-0.29, 0.41);
                    float2 b = float2(-0.10, 0.16);
                    float2 c = float2(-0.16, 0.04);
                    float2 d = float2(0.07, -0.10);
                    float2 e = float2(0.04, -0.25);
                    float2 f = float2(0.29, -0.41);
                    tear = max(Stroke(fracture, a, b, 0.008), Stroke(fracture, b, c, 0.015));
                    tear = max(tear, Stroke(fracture, c, d, 0.023));
                    tear = max(tear, Stroke(fracture, d, e, 0.017));
                    tear = max(tear, Stroke(fracture, e, f, 0.008));
                    tear = max(tear, Stroke(fracture, d, float2(0.28, 0.06), 0.006));
                    tear = max(tear, Stroke(fracture, e, float2(-0.065, -0.36), 0.004));
                    rim = Stroke(fracture, c + float2(0, -0.023), d + float2(0, -0.023), 0.013);
                    rim = max(rim, Stroke(fracture, d + float2(0.021, 0), e + float2(0.021, 0), 0.009));
                }

                float metal = saturate(lip * 0.36 + chips * 0.67 + rim * 0.83) * (1 - tear);
                float dark = max(shade, tear * 0.98);
                float opacity = saturate(dark + metal);
                clip(opacity - 0.015);
                float3 color = lerp(float3(0.028, 0.026, 0.024), float3(0.53, 0.56, 0.57), metal / opacity);
                return half4(color, opacity);
            }
            ENDHLSL
        }
    }
}
