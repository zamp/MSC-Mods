Shader "WindowDirtShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_WiperHole ("Wiper Hole", 2D) = "white" {}
		_Cutoff ("Cutoff", Range(0,1)) = 0
		_WipeAmount ("Wiper Cutoff", Range(0,1)) = 0
		_Offset ("Offset", int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Back
		Offset 0, [_Offset]

		CGPROGRAM
		#pragma surface surf BlinnPhong alphatest:_Cutoff addshadow fullforwardshadows
 
		struct Input {
			float2 uv_MainTex;
		};

		sampler2D _MainTex;
		half2 _MainTex_TexelSize;
		sampler2D _WiperHole;
		float _WipeAmount;
 
		void surf (Input IN, inout SurfaceOutput o)
		{
			// is this dirt or not
			fixed4 dirt = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 hole = tex2D(_WiperHole, IN.uv_MainTex);

			o.Albedo = dirt;
			o.Specular = 0;
			//o.Smoothness = 0;
			o.Alpha = dirt.a - (_WipeAmount * (1 - hole.a));
			o.Emission = 0;
			o.Normal;
		}
		ENDCG
	}
	Fallback "Unlit/TransparentCutout"
}
