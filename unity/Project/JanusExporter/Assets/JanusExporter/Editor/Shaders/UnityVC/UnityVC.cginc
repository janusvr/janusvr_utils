/*
Unity Standard Vertex Color Shader Lib v0.92.1 (for Unity 5.5.0f3 and 5.6)
by defaxer
*/

#ifndef UNITY_VC_INCLUDED
#define UNITY_VC_INCLUDED

#if defined(_VERTEXCOLOR) || defined(_VERTEXCOLOR_LERP)
float _IntensityVC;
#endif

uniform float4 _LightMapUV;
uniform sampler2D _LightMapTex;

struct VertexInput_VC
{
	float4 vertex	: POSITION;
#if defined(_VERTEXCOLOR) || defined(_VERTEXCOLOR_LERP)
	fixed4 color : COLOR;
#endif
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
};

float4 TexCoords_VC(VertexInput_VC v)
{
	float4 texcoord;
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
	return texcoord;
}

inline half4 VertexGIForward_VC(VertexInput_VC v, float3 posWorld, half3 normalWorld)
{
	half4 ambientOrLightmapUV = 0;
	// Static lightmaps
#ifndef LIGHTMAP_OFF
	ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	ambientOrLightmapUV.zw = 0;
	// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
#elif UNITY_SHOULD_SAMPLE_SH
#ifdef VERTEXLIGHT_ON
	// Approximated illumination from non-important point lights
	ambientOrLightmapUV.rgb = Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, posWorld, normalWorld);
#endif

	ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, ambientOrLightmapUV.rgb);
#endif

#ifdef DYNAMICLIGHTMAP_ON
	ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

	return ambientOrLightmapUV;
}

//Forward Pass
struct VertexOutputForwardBase_VC
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;

#if UNITY_VERSION >= 506
	half4 tangentToWorldAndPackedData[3] : TEXCOORD2;
#else
	half4 tangentToWorldAndParallax[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
#endif

	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	SHADOW_COORDS(6)
		UNITY_FOG_COORDS(7)
#if defined(_VERTEXCOLOR) || defined(_VERTEXCOLOR_LERP)
		fixed4 color : COLOR;
#endif
	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	float3 posWorld					: TEXCOORD8;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
#if UNITY_SPECCUBE_BOX_PROJECTION
	half3 reflUVW				: TEXCOORD9;
#else
	half3 reflUVW				: TEXCOORD8;
#endif
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardBase_VC vertForwardBase_VC(VertexInput_VC v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputForwardBase_VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase_VC, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
#if UNITY_SPECCUBE_BOX_PROJECTION || UNITY_LIGHT_PROBE_PROXY_VOLUME
	o.posWorld = posWorld.xyz;
#endif
	float2 inTexCoord = v.uv1;
	float2 screenPosTexCoord = float2(inTexCoord.x - 0.5f, -inTexCoord.y + 0.5f) * 2;
	o.pos = float4(screenPosTexCoord, 0, 1);
	o.ambientOrLightmapUV.xy = v.uv1.xy * _LightMapUV.xy + _LightMapUV.zw;
	//o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords_VC(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);

#if UNITY_VERSION >= 506
	o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
#endif

#else

#if UNITY_VERSION >= 506
	o.tangentToWorldAndPackedData[0].xyz = 0;
	o.tangentToWorldAndPackedData[1].xyz = 0;
	o.tangentToWorldAndPackedData[2].xyz = normalWorld;
#else
	o.tangentToWorldAndParallax[0].xyz = 0;
	o.tangentToWorldAndParallax[1].xyz = 0;
	o.tangentToWorldAndParallax[2].xyz = normalWorld;
#endif

#endif
	//We need this for shadow receving
	TRANSFER_SHADOW(o);

	o.ambientOrLightmapUV = VertexGIForward_VC(v, posWorld, normalWorld);

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
	o.tangentToWorldAndParallax[0].w = viewDirForParallax.x;
	o.tangentToWorldAndParallax[1].w = viewDirForParallax.y;
	o.tangentToWorldAndParallax[2].w = viewDirForParallax.z;
#endif

#if UNITY_OPTIMIZE_TEXCUBELOD
	o.reflUVW = reflect(o.eyeVec, normalWorld);
#endif

	//UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

half4 fragForwardBase_VC(VertexOutputForwardBase_VC i) : SV_Target
{
	FRAGMENT_SETUP(s)
#if UNITY_OPTIMIZE_TEXCUBELOD
	s.reflUVW = i.reflUVW;
#endif

UnityLight mainLight = MainLight();// (s.normalWorld);
half atten = SHADOW_ATTENUATION(i);

half occlusion = Occlusion(i.tex.xy);
UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
#if _VERTEXCOLOR
c *= i.color * _IntensityVC;
#endif
#if _VERTEXCOLOR_LERP
c *= lerp(half4(1,1,1,1), i.color, _IntensityVC);
#endif
c.rgb += UNITY_BRDF_GI(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
c.rgb += Emission(i.tex.xy); //i.color.a * s.diffColor;

//UNITY_APPLY_FOG(i.fogCoord, c.rgb);

return OutputForward(c, s.alpha);
}

//  Additive forward pass (one light per pass)
struct VertexOutputForwardAdd_VC
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	half3 eyeVec 						: TEXCOORD1;
	half4 tangentToWorldAndLightDir[3]	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:lightDir]
	LIGHTING_COORDS(5, 6)
		UNITY_FOG_COORDS(7)

		// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
		half3 viewDirForParallax			: TEXCOORD8;
#else
		float4 posWorld : TEXCOORD8;
#endif
	fixed4 color : COLOR;

	UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAdd_VC vertForwardAdd_VC(VertexInput_VC v)
{
	VertexOutputForwardAdd_VC o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd_VC, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	float2 inTexCoord = v.uv1;
	float2 screenPosTexCoord = float2(inTexCoord.x - 0.5f, -inTexCoord.y + 0.5f) * 2;
	o.pos = float4(screenPosTexCoord, 0, 1);
	o.tex.xy = v.uv1.xy * _LightMapUV.xy + _LightMapUV.zw;
	//o.pos = UnityObjectToClipPos(v.vertex);

	o.tex = TexCoords_VC(v);
	o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);
#ifdef _TANGENT_TO_WORLD
	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
#else
	o.tangentToWorldAndLightDir[0].xyz = 0;
	o.tangentToWorldAndLightDir[1].xyz = 0;
	o.tangentToWorldAndLightDir[2].xyz = normalWorld;
#endif
	//We need this for shadow receving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = NormalizePerVertexNormal(lightDir);
#endif
	o.tangentToWorldAndLightDir[0].w = lightDir.x;
	o.tangentToWorldAndLightDir[1].w = lightDir.y;
	o.tangentToWorldAndLightDir[2].w = lightDir.z;

#ifdef _PARALLAXMAP
	TANGENT_SPACE_ROTATION;
	o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
#endif

#ifdef _VERTEXCOLOR
	o.color = lerp(fixed4(1, 1, 1, 1), fixed4(1, 1, 1, 1) * v.color, _IntensityVC);
#endif

	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

half4 fragForwardAdd_VC(VertexOutputForwardAdd_VC i) : SV_Target
{
	FRAGMENT_SETUP_FWDADD(s)

	UnityLight light = AdditiveLight(IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
UnityIndirect noIndirect = ZeroIndirect();

half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);
c *= i.color;

UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
return OutputForward(c, s.alpha);
}

#endif