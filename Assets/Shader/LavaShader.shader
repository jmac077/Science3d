Shader "Erosion/LavaShader" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (0,0,1,1)
		_MinLavaHt("MinLavaHt", Float) = 1.0
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma exclude_renderers gles
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0
		#pragma glsl

		sampler2D _MainTex;
		float4 _Color;
		float _MinLavaHt;

		uniform sampler2D _LavaField, _VelocityField;
		uniform float _ScaleY, _Layers, _TexSize;
		
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
		
		void vert(inout appdata_full v,out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			v.tangent = float4(1,0,0,1);
		
			v.vertex.y += GetTotalHeight(tex2Dlod(_MainTex, float4(v.texcoord.xy, 0.0, 0.0))) * _ScaleY;
			v.vertex.y += tex2Dlod(_LavaField, float4(v.texcoord.xy, 0.0, 0.0)).x * _ScaleY;
		}
		
		float3 FindNormal(float2 uv, float u)
        {

        	float ht0 = GetTotalHeight(tex2D(_MainTex, uv + float2(-u, 0)));
            float ht1 = GetTotalHeight(tex2D(_MainTex, uv + float2(u, 0)));
            float ht2 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, -u)));
            float ht3 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, u)));
      
            ht0 += tex2D(_LavaField, uv + float2(-u, 0)).x;
            ht1 += tex2D(_LavaField, uv + float2(u, 0)).x;
            ht2 += tex2D(_LavaField, uv + float2(0, -u)).x;
            ht3 += tex2D(_LavaField, uv + float2(0, u)).x;
            
            float2 _step = float2(1.0, 0.0);

            float3 va = normalize(float3(_step.xy, ht1-ht0));
            float3 vb = normalize(float3(_step.yx, ht2-ht3));

           return cross(va,vb);
        }

		void surf(Input IN, inout SurfaceOutput o) 
		{
			float3 n = FindNormal(IN.uv_MainTex, 1.0/_TexSize);
			
			float ht = tex2D(_LavaField, IN.uv_MainTex).x;
				if(ht < _MinLavaHt) discard;
				
			o.Albedo =_Color.rgb;
			o.Alpha = 1.0;
			o.Normal = n;	
			
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
