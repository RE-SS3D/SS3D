Shader "Custom/InteractionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 8)) = 2
        // Fill mode for flat tiles (floor, plating), whose normals face the camera so a hull shows nothing:
        // InteractionOutlineView sets Cull Back, a negative offset and alpha blend on a second material.
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 1
        _OffsetFactor ("Offset Factor", Float) = 1
        _OffsetUnits ("Offset Units", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        // Drawn after opaques and never writes depth. The selection pick pass reuses the scene
        // depth buffer after opaques; a depth-writing hull (it used to be Geometry-1 / ZWrite On)
        // poked in front of the hovered object's own surface at concave parts and grazing angles,
        // so the object failed its own pick, lost hover, dropped the outline and regained hover the
        // next frame: outline and examine tooltip flickered. Without depth writes the hull can only
        // show where nothing nearer was drawn, and the pick pass never sees it.
        //
        // The hull is pushed out in clip space by a fixed number of screen pixels along the projected normal, so the
        // outline stays a thin, even line at any distance and on any object scale. (It used to grow 0.03 object units
        // along the normal, which read as a thick shell up close and on large objects.)

        Pass
        {
            Name "InteractionOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Offset [_OffsetFactor], [_OffsetUnits]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy;
                float normalLength = length(normalCS);
                if (normalLength > 1e-5)
                {
                    // NDC spans 2 units across the target; multiply by w to undo the perspective divide.
                    float2 pixelToNdc = 2.0 / _ScreenParams.xy;
                    positionCS.xy += (normalCS / normalLength) * _OutlineWidth * pixelToNdc * positionCS.w;
                }

                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
