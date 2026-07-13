Shader "Custom/AtmosDistortion"
{
    Properties
    {
        _AtmosDistortionStrength ("Distortion Strength", Float) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AtmosDistortion"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AtmosVert
            #pragma fragment AtmosDistortionFrag

            #include "AtmosCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

            TEXTURE2D_X(_BlitTexture);

            half4 AtmosDistortionFrag(AtmosVaryings input) : SV_Target
            {
                float2 uvScreen = input.uv;
                float2 uvSample = uvScreen;
#if UNITY_UV_STARTS_AT_TOP
                uvSample.y = 1.0 - uvSample.y;
#endif

                float depth = SampleSceneDepth(uvSample);
                bool isSky = depth <= 0.0;
                float3 centerColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvSample).rgb;
                float2 distortion = AtmosEvaluateDistortionOffset(uvScreen, depth, isSky);
                if (length(distortion) <= 1e-5)
                    return half4(centerColor, 1.0);

                float2 distortedUV = clamp(uvSample + distortion, 0.0, 1.0);
                float3 distortedColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    distortedUV).rgb;

                // Heat shimmer should warp bright fire, not pull in black border/background pixels.
                float centerLum = dot(centerColor, float3(0.299, 0.587, 0.114));
                float distortedLum = dot(distortedColor, float3(0.299, 0.587, 0.114));
                float3 sceneColor = distortedLum < centerLum * 0.82 ? centerColor : distortedColor;

                return half4(sceneColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
