#ifndef TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED
#define TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED

struct Input
{
	float2 uv_Splat0 : TEXCOORD0;
	float2 uv_Splat1 : TEXCOORD1;
	float2 uv_Splat2 : TEXCOORD2;
	float2 uv_Splat3 : TEXCOORD3;
	float2 tc_Control : TEXCOORD4;	// Not prefixing '_Contorl' with 'uv' allows a tighter packing of interpolators, which is necessary to support directional lightmap.
	UNITY_FOG_COORDS(5)
};

sampler2D _LandTex;
float3 _LayerColor0, _LayerColor1, _LayerColor2, _LayerColor3;
uniform float _ScaleY, _Layers, _TexSize;

sampler2D _Control;
float4 _Control_ST;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;

#ifdef _TERRAIN_NORMAL_MAP
	sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
#endif

float GetTotalHeight(float4 texData) {
	float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
	float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
	return dot(texData, addVec);
}

void SplatmapVert(inout appdata_full v)
{
    v.vertex.y += GetTotalHeight(tex2Dlod(_LandTex, float4(v.texcoord.xy, 0.0, 0.0))) * _ScaleY;

    v.tangent.xyz = cross(v.normal, float3(0,0,1));
    v.tangent.w = -1;
}

#ifdef TERRAIN_STANDARD_SHADER
void SplatmapMix(Input IN, half4 defaultAlpha, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
#else
void SplatmapMix(Input IN, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
#endif
{
	splat_control = tex2D(_Control, IN.tc_Control);
	weight = dot(splat_control, half4(1,1,1,1));

	#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
		clip(weight - 0.0039 /*1/255*/);
	#endif

	// Normalize weights before lighting and restore weights in final modifier functions so that the overal
	// lighting result can be correctly weighted.
	splat_control /= (weight + 1e-3f);

	mixedDiffuse = 0.0f;
	#ifdef TERRAIN_STANDARD_SHADER
		mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0) * half4(1.0, 1.0, 1.0, defaultAlpha.r);
		mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1) * half4(1.0, 1.0, 1.0, defaultAlpha.g);
		mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2) * half4(1.0, 1.0, 1.0, defaultAlpha.b);
		mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3) * half4(1.0, 1.0, 1.0, defaultAlpha.a);
	#else
		mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0);
		mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1);
		mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2);
		mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3);
	#endif

	#ifdef _TERRAIN_NORMAL_MAP
		fixed4 nrm = 0.0f;
		nrm += splat_control.r * tex2D(_Normal0, IN.uv_Splat0);
		nrm += splat_control.g * tex2D(_Normal1, IN.uv_Splat1);
		nrm += splat_control.b * tex2D(_Normal2, IN.uv_Splat2);
		nrm += splat_control.a * tex2D(_Normal3, IN.uv_Splat3);
		mixedNormal = UnpackNormal(nrm);
	#endif
}

#ifndef TERRAIN_SURFACE_OUTPUT
	#define TERRAIN_SURFACE_OUTPUT SurfaceOutput
#endif

void SplatmapFinalColor(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 color)
{
	color *= o.Alpha;
	#ifdef TERRAIN_SPLAT_ADDPASS
		UNITY_APPLY_FOG_COLOR(IN.fogCoord, color, fixed4(0,0,0,0));
	#else
		UNITY_APPLY_FOG(IN.fogCoord, color);
	#endif
}

void SplatmapFinalPrepass(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 normalSpec)
{
	normalSpec *= o.Alpha;
}

void SplatmapFinalGBuffer(Input IN, TERRAIN_SURFACE_OUTPUT o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
{
	diffuse.rgb *= o.Alpha;
	specSmoothness *= o.Alpha;
	normal.rgb *= o.Alpha;
	emission *= o.Alpha;
}

#endif // TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED
