Shader "Hidden/LMapUnpacked"
{
	Properties
	{
		_LightMapUV("LightMapUV", Vector) = (0,0,0,0)
		_LightMapTex("LightMapTex", 2D) = "White" {}
	}
	SubShader
	{
		Cull Off
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
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			uniform float4 _LightMapUV;
			uniform sampler2D _LightMapTex;

			v2f vert(appdata v)
			{
				v2f o;
				float2 inTexCoord = v.uv1;
				float2 screenPosTexCoord = float2(inTexCoord.x - 0.5f, -inTexCoord.y + 0.5f) * 2;
				o.vertex = float4(screenPosTexCoord, 0, 1);
				o.uv0 = v.uv0;
				o.uv1.xy = v.uv1.xy * _LightMapUV.xy + _LightMapUV.zw;

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float3 lightmap = tex2D(_LightMapTex, i.uv1);
				//float3 lightmap = DecodeLightmap(tex2D(_LightMapTex, i.uv1));

				return float4(lightmap, 1);
			}
			ENDCG
		}
	}
}
