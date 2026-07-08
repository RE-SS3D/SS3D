Shader "Custom/AtmosGlow"
{
    Properties
    {
        _AtmosPlasmaEmissionColor ("Plasma Emission Color", Color) = (0.75, 0.2, 1.0, 1)
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
            Name "AtmosGlow"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AtmosVert
            #pragma fragment AtmosGlowFrag

            #include "AtmosCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

            TEXTURE2D_X(_BlitTexture);

            half4 AtmosGlowFrag(AtmosVaryings input) : SV_Target
            {
                float2 uvScreen = input.uv;
                float2 uvSample = uvScreen;
#if UNITY_UV_STARTS_AT_TOP
                uvSample.y = 1.0 - uvSample.y;
#endif

                float3 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvSample).rgb;
                float depth = SampleSceneDepth(uvSample);
                bool isSky = depth <= 0.0;

                float3 glow = AtmosEvaluateGlow(uvScreen, depth, isSky);
                return half4(sceneColor + glow, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
