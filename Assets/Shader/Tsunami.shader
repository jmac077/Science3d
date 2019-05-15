
Shader "Erosion/Tsunami" 
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
			uniform float _Z, _Amount;
		
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
			
			float4 frag(v2f IN) : COLOR
			{	
				float waterAmount = 0;

				if(IN.pos.x <= 30 &&  IN.pos.x >= 10){
					waterAmount = 50;
				}
				
				return tex2D(_MainTex, IN.uv) + float4(waterAmount,0,0,0);
			}
			
			ENDCG

    	}
	}
}