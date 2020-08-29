Shader "Unlit/singleColourShader"
{
    Properties {
    }
    SubShader {
        
        Pass {
		
            CGPROGRAM
			
            #pragma vertex vert
            #pragma fragment frag
            
            struct VertInput {
                float4 pos : POSITION;
            };
			uniform float3 colour;

            struct VertOutput {
                float4 pos : SV_POSITION;
				float3 colour : COLOR;
            };

            VertOutput vert (VertInput i){
                VertOutput o;
                o.pos = UnityObjectToClipPos(i.pos);
				o.colour = colour;
                return o;
            }

            fixed4 frag (VertOutput i) : COLOR {
                return fixed4(i.colour, 1.0f);
            }
            ENDCG
        }
    }
}
