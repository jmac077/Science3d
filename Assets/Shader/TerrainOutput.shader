
Shader "Erosion/TerrainOutput" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader 
	{
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			uniform float2 _Point;
			uniform float _Radius, _Amount;
			uniform int _Layer;

			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float GetGaussFactor(float2 diff, float rad2) 
			{
				return exp(-(diff.x*diff.x+diff.y*diff.y)/rad2);
			}
			
			float4 frag(v2f IN) : COLOR
			{
				float gauss = GetGaussFactor(_Point - IN.uv, _Radius*_Radius);
				
				float terrainAmount = gauss * _Amount;
				float finalAmnt = terrainAmount;
				float4 use;
				if(_Layer == 0){
					use = float4(1,0,0,0);
					if(_Amount>0)
					finalAmnt = min(tex2D(_MainTex, IN.uv).x,terrainAmount);;
				}
				if(_Layer == 1){
					use = float4(0,1,0,0);
					if(_Amount>0)
					finalAmnt = min(tex2D(_MainTex, IN.uv).y,terrainAmount);
				}
				if(_Layer == 2){
					use = float4(0,0,1,0);
					if(_Amount>0)
					finalAmnt = min(tex2D(_MainTex, IN.uv).z,terrainAmount);
				}

				return tex2D(_MainTex, IN.uv) - finalAmnt*use;
			}
			
			ENDCG

    	}
	}
}