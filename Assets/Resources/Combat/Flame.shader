Shader "SS3D/Combat/Flame"
{
    // Camera-facing particle fire. Shape and heat come from per-particle scrolling noise, so neighbouring
    // billboards never repeat and the fire reads as volume rather than a stack of cards.
    // Particle streams: POSITION, COLOR, TEXCOORD0.xy = UV, TEXCOORD0.z = StableRandom.x, TEXCOORD0.w = AgePercent.
    Properties
    {
        _Intensity ("Intensity", Float) = 1
        _Mode ("0 jet blob, 1 surface tongue, 2 smoke, 3 ember, 4 heat haze", Float) = 0
        _Distortion ("Heat haze strength", Float) = 0.012
        _Absorb ("Background absorption", Range(0, 1)) = 0.2
        _SoftDistance ("Soft depth fade (m)", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            // Premultiplied: fire is mostly additive glow; smoke absorbs fully.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _Mode;
                float _Absorb;
                float _SoftDistance;
                float _Distortion;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float4 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 screen : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.screen = position.positionNDC;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float sum = 0, amp = 0.5;
                for (int i = 0; i < 4; i++) { sum += Noise(p) * amp; p = p * 2.03 + 17.1; amp *= 0.5; }
                return sum;
            }
            // Black-body-ish ramp: dark red embers, orange body, yellow-white core.
            half3 Heat(float heat)
            {
                half3 c = lerp(half3(0.35, 0.03, 0.0), half3(1.0, 0.32, 0.02), smoothstep(0.0, 0.4, heat));
                c = lerp(c, half3(1.0, 0.72, 0.22), smoothstep(0.35, 0.7, heat));
                return lerp(c, half3(1.0, 0.95, 0.8), smoothstep(0.75, 1.0, heat));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv.xy;
                float seed = input.uv.z * 63.7;
                float age = input.uv.w;
                float t = _Time.y;
                float2 p = uv * 2 - 1;

                float2 screenUV = input.screen.xy / input.screen.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float soft = saturate((sceneDepth - input.positionCS.w) / max(_SoftDistance, 0.001));

                if (_Mode > 3.5)
                {
                    // Heat haze: refract the opaque scene with rising noise. Drawn before the fire (lower queue)
                    // because the opaque copy holds no transparents; flames then render on top of the shimmer.
                    float r = saturate(1 - length(p));
                    float mask = r * r * input.color.a * soft;
                    float2 flow = float2(uv.x * 3 + seed, uv.y * 2.5 - t * 2.4);
                    float2 offset = float2(Noise(flow) - 0.5, Noise(flow + 31.7) - 0.5);
                    // Scale by inverse depth so the shimmer keeps a world size as the camera zooms.
                    float2 sampleUV = screenUV + offset * _Distortion * mask * (6 / max(input.positionCS.w, 1));
                    half3 scene = SampleSceneColor(screenUV);
                    half3 bent = SampleSceneColor(sampleUV);
                    // Don't pull dark pixels into bright fire light (same guard as the atmos distortion pass).
                    half lumScene = dot(scene, half3(0.299, 0.587, 0.114));
                    half lumBent = dot(bent, half3(0.299, 0.587, 0.114));
                    bent = lumBent < lumScene * 0.8 ? lerp(scene, bent, 0.35) : bent;
                    return half4(bent * mask, mask);
                }

                if (_Mode > 2.5)
                {
                    float disc = saturate(1 - length(p));
                    float glow = disc * disc;
                    half3 c = Heat(1.1 - age) * glow * _Intensity * input.color.a;
                    return half4(c * soft, 0);
                }

                // Noise scrolls upward through the quad; warp first so tongues lick and split.
                float warp = Fbm(float2(uv.x * 2.2 + seed, uv.y * 1.8 - t * 1.9)) - 0.5;
                float detail = Fbm(float2(uv.x * 4.5 + warp * 1.2 + seed, uv.y * 3.5 - t * 3.1));

                if (_Mode > 1.5)
                {
                    float r = length(p + warp * 0.5);
                    float density = saturate(1 - r) * lerp(0.4, 1.0, detail);
                    float a = smoothstep(0.05, 0.6, density) * input.color.a * soft;
                    return half4(input.color.rgb * a, a);
                }

                float density;
                if (_Mode > 0.5)
                {
                    // Tongue: wide hot base narrowing to flickering tips; warp grows with height.
                    float h = uv.y;
                    float x = p.x + warp * 0.9 * h;
                    float width = lerp(0.9, 0.12, pow(h, 0.75));
                    density = saturate(1 - abs(x) / width) * smoothstep(0.0, 0.18, h) * pow(saturate(1 - h), 0.5);
                }
                else
                {
                    density = saturate(1 - length(p + warp * 0.6));
                }
                float heat = saturate(density * 2.0 - detail * 0.6 - age * 0.45);
                float mask = smoothstep(0.02, 0.28, heat) * input.color.a * soft;
                half3 color = Heat(heat) * input.color.rgb * _Intensity * mask;
                // Soft glow fills the gaps between neighbouring tongues so the fire reads as one body.
                float halo = density * density * (1 - age * 0.6) * input.color.a * soft;
                color += half3(1.0, 0.38, 0.06) * halo * _Intensity * 0.3;
                return half4(color, mask * _Absorb);
            }
            ENDHLSL
        }
    }
}
