Shader "DirtShader"
{
	Properties
	{
		 _Color ("Color", Color) = (1,1,1,1)
		 _MainTex ("Albedo", 2D) = "white" { }
		 _DirtTex ("Dirt Texture", 2D) = "white" { }
		 _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
		 _Glossiness ("Smoothness", Range(0,1)) = 0.5
		 _SpecColor ("Specular", Color) = (0.2,0.2,0.2,1)
		 _SpecGlossMap ("Specular", 2D) = "white" { }
		 _BumpScale ("Scale", Float) = 1
		 _BumpMap ("Normal Map", 2D) = "bump" { }
		 _Parallax ("Height Scale", Range(0.005,0.08)) = 0.02
		 _ParallaxMap ("Height Map", 2D) = "black" { }
		 _OcclusionStrength ("Strength", Range(0,1)) = 1
		 _OcclusionMap ("Occlusion", 2D) = "white" { }
		 _EmissionColor ("Color", Color) = (0,0,0,1)
		 _EmissionMap ("Emission", 2D) = "white" { }
		 _DetailMask ("Detail Mask", 2D) = "white" { }
		 _DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" { }
		 _DetailNormalMapScale ("Scale", Float) = 1
		 _DetailNormalMap ("Normal Map", 2D) = "bump" { }
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Back
		LOD 100

		CGPROGRAM
		#pragma surface surf StandardSpecular addshadow
 
		sampler2D _MainTex;
		sampler2D _DirtTex;
		sampler2D _DetailMask;
		sampler2D _MetallicGlossMap;
		sampler2D _DetailAlbedoMap;
		sampler2D _SpecGlossMap;
		sampler2D _OcclusionMap;
		float4 _Color;
		float _Shininess;
		float _Cutoff;
		float _Metallic;
		float _Smoothness;
		float _Glossiness;
		float _OcclusionStrength;
 
		struct Input {
			float2 uv_MainTex;
			float2 uv_DetailMask;
			float2 uv_DetailAlbedoMap;
			float2 uv_MetallicGlossMap;
			float2 uv_SpecGlossMap;
			float2 uv_OcclusionMap;
		};
 
		void surf (Input IN, inout SurfaceOutputStandardSpecular o)
		{
			half4 tex = tex2D(_MainTex, IN.uv_MainTex);				
			half4 dirt = tex2D(_DirtTex, IN.uv_MainTex);
			half4 gloss = tex2D(_SpecGlossMap, IN.uv_SpecGlossMap);

			half4 col = tex * _Color;

			o.Specular = _SpecColor * gloss;
			o.Smoothness = _Glossiness;
			o.Occlusion = tex2D(_OcclusionMap, IN.uv_OcclusionMap) * _OcclusionStrength;

			//half4 mask = tex2D(_DetailMask, IN.uv_DetailMask);
			//half4 maskmap = tex2D(_DetailAlbedoMap, IN.uv_DetailAlbedoMap);
			col.rgb *= tex2D(_DetailAlbedoMap, IN.uv_DetailAlbedoMap).rgb * 2;

			if (dirt.a > _Cutoff)
			{
				col.rgb = dirt.rgb;
				col.a = 1;
				o.Smoothness = 0;
			}
   
			o.Albedo = col;
			o.Alpha = 1;
		}
		ENDCG
	}
	Fallback "Diffuse"
}
