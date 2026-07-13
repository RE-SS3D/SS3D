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
                float2 distortion = AtmosEvaluateDistortionOffset(uvScreen, depth, isSky);
                float3 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uvSample + distortion).rgb;

                return half4(sceneColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
