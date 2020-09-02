Shader "Unlit/singleColourShader"
{
    Properties
    {
        // we have removed support for texture tiling/offset,
        // so make them not be displayed in material inspector
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        
        Pass {
		
            CGPROGRAM
			
            #pragma vertex vert
            #pragma fragment frag
            
            struct VertInput {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0; // texture coordinate

            };
			uniform float3 colour;

            struct VertOutput {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 pos : SV_POSITION;
				float3 colour : COLOR;
            };

            VertOutput vert (VertInput i){
                VertOutput o;
                o.pos = UnityObjectToClipPos(i.pos);
				o.colour = colour;
				o.uv = i.uv;
                return o;
            }
			
            // texture we will sample
            sampler2D _MainTex;
			
            fixed4 frag (VertOutput i) : COLOR {
				fixed4 sampledColour = tex2D(_MainTex, i.uv);
				float alphaValue = sampledColour.a;
				if (alphaValue < 1.0f)
				{
					alphaValue = 0.0f;
				}
                return fixed4(i.colour, alphaValue);
            }
            ENDCG
        }
    }
}
