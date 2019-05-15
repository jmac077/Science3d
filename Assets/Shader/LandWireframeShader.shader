
Shader "Erosion/LandWireframeShader" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_WireColor("WireColor", Color) = (1,1,1,0.1)
	}
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha 
		
		CGPROGRAM
		#pragma exclude_renderers gles
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0
		#pragma glsl

		sampler2D _MainTex;
		uniform float _ScaleY, _Layers;
		uniform float4 _WireColor;
		
		struct Input 
		{
			float2 uv_MainTex;
		};
		
		float GetTotalHeight(float4 texData) 
		{
			float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
			float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
			return dot(texData, addVec);
		}
		
		void vert(inout appdata_full v) 
		{
			v.vertex.y += GetTotalHeight(tex2Dlod(_MainTex, float4(v.texcoord.xy, 0.0, 0.0))) * _ScaleY;
		}

		void surf(Input IN, inout SurfaceOutput o) 
		{
			o.Albedo = _WireColor.rgb;
			o.Alpha = _WireColor.a;
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
