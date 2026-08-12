Shader "Vision/VisionMaskBlur"
{
    //TODO Clean this up for multiplatform and add comments
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "VisionCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2D(_MainTex);
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex.xy, 0, 1);
                o.uv = (v.vertex.xy + 1) * 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif
                return o;
            }

            float _FovBlurQuality; //Angular samples
            float _FovBlurDirections; //Linear samples
            float2 _FovBlurSize;
            UNITY_DECLARE_TEX2D(_FovTex);
            float4 frag(v2f IN) : SV_Target 
            {
                float2 diameter = (_FovBlurSize / _ScreenParams.xy) * 2;
                float average;
                float3 thisPos = ClipToWorld(IN.uv * 2 - 1);
                float3 thisDepth = thisPos - _WorldSpaceCameraPos;
                float thisVisible = length(UNITY_SAMPLE_TEX2D(_FovTex, IN.uv)) > 0.5;
                float totalSamples = 0;
                for(float angle = 0; angle < UNITY_TWO_PI; angle += UNITY_TWO_PI / _FovBlurDirections)
                {
                    for(float sample = 1; sample <= _FovBlurQuality; sample++)
                    {
                        float2 offset = float2(cos(angle),sin(angle)) * diameter * (sample / _FovBlurQuality);
                        float2 sampleUV = IN.uv + offset;
                        float3 samplePos = ClipToWorld(sampleUV * 2 - 1);
                        float3 sampleDepth = samplePos - _WorldSpaceCameraPos;
                        if((!thisVisible || length(offset) < 0.006 || (length(thisDepth - sampleDepth) < 0.5)))
                        {
                            totalSamples += 1;
                            if(length(UNITY_SAMPLE_TEX2D(_FovTex, sampleUV)) < 0.5)
                            {
                                average += 1;
                            }
                        }
                    }
                }
                average /= totalSamples;
                float4 col = UNITY_SAMPLE_TEX2D(_MainTex, IN.uv);
                
                if(!thisVisible) //Check if location is masked
                {
                    col = lerp (col, float4(0, 0, 0, 1), 0.2);
                }
                
                if(average > 0)
                {
                    col = float4(0,0,0,1) * average + col * (1 - average);
                }
                return col;
            }

            ENDCG
        }
    }
}
