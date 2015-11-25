Shader "Kinect/BodyShader" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "black" {}
    }
    
	SubShader {
		Pass {

			CGPROGRAM
			#pragma target 5.0
			#pragma enable_d3d11_debug_symbols

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float _TexResX;
			uniform float _TexResY;

			StructuredBuffer<float> _BodyIndexBuffer;

			struct v2f {
				float4 pos : SV_POSITION;
			    float2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				//o.uv.x = 1 - v.texcoord.x;
				
				return o;
			}

			float4 frag (v2f i, in uint id : SV_InstanceID) : COLOR
			{
				int dx = (int)(i.uv.x * _TexResX);
				int dy = (int)(i.uv.y * _TexResY);
				int di = (int)(dx + dy * _TexResX);
				
				float player = _BodyIndexBuffer[di];
				if (player != 255)
				{
					float clrPlayer = (240 + player) / 255;
					return float4(clrPlayer, clrPlayer, clrPlayer, clrPlayer);
				}
				
				return float4(0, 0, 0, 0);
			}

			ENDCG
		}
	}

	Fallback Off
}