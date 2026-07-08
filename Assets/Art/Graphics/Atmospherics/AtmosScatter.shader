Shader "Custom/AtmosScatter"
{
    Properties
    {
        _AtmosScatterColor ("Scatter Color", Color) = (0.65, 0.7, 0.75, 1)
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
            Name "AtmosScatter"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AtmosVert
            #pragma fragment AtmosFrag

            // AtmosCommon pulls in core Core.hlsl/Common.hlsl (which define the SAMPLER macro).
            // GlobalSamplers.hlsl (sampler_LinearClamp) must come after so SAMPLER is already defined.
            #include "AtmosCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

            // Scene color copied into this texture by the render feature before the scatter draw.
            TEXTURE2D_X(_BlitTexture);

            half4 AtmosFrag(AtmosVaryings input) : SV_Target
            {
                // uvScreen: NDC-aligned (bottom-left origin), used for world-space reconstruction.
                // uvSample: texture-space, flipped on platforms whose render textures start at top.
                float2 uvScreen = input.uv;
                float2 uvSample = uvScreen;
#if UNITY_UV_STARTS_AT_TOP
                uvSample.y = 1.0 - uvSample.y;
#endif

                float3 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvSample).rgb;

                float depth = SampleSceneDepth(uvSample);
                bool isSky = (_AtmosDebugView == 0) && (depth <= 0.0);

                float3 worldPos = isSky
                    ? AtmosComputeWorldPosition(uvScreen, 0.0)
                    : AtmosComputeWorldPositionFromDepth(uvScreen, depth);

                // In debug views, project onto the tile plane so mask lookup is stable and
                // independent of whichever surface wrote depth (floors/plenum tops/wall tops).
                float3 debugWorldPos = worldPos;
                if (_AtmosDebugView != 0)
                    debugWorldPos.xz = AtmosComputeWorldXZOnPlane(uvScreen, 0.0);

                float4 debugColor = AtmosDebugColor(debugWorldPos);
                if (debugColor.a >= 0.0)
                    return half4(lerp(sceneColor, debugColor.rgb, debugColor.a), 1.0);

                float fog = AtmosEvaluateScatter(uvScreen, depth, isSky);
                float transmittance = 1.0 - fog;
                float3 result = sceneColor * transmittance + _AtmosScatterColor.rgb * fog;

                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
