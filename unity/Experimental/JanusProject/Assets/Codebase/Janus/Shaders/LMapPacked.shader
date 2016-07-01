Shader "Hidden/LMapPacked"
{
	Properties
	{
		_LightMapUV("LightMapUV", Vector) = (0,0,0,0)
		_LightMapTex("LightMapTex", 2D) = "White" {}
		_MainTex("MainTexture", 2D) = "White" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv1 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			uniform float4 _LightMapUV;
			uniform sampler2D _LightMapTex;

			v2f vert(appdata v)
			{
				v2f o;
				float2 inTexCoord = v.uv1 * _LightMapUV.xy + _LightMapUV.zw;
				o.uv1 = inTexCoord;

				float2 screenPosTexCoord = float2(inTexCoord.x - 0.5f, -inTexCoord.y + 0.5f) * 2;
				o.vertex = float4(screenPosTexCoord, 0, 1);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 lightData = tex2D(_LightMapTex, i.uv1);
				float3 lightmap = 2 * DecodeLightmap(lightData);
				return half4(lightmap.rgb, 1);
			}
			ENDCG
		}
	}
}
