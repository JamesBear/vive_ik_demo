Shader "Morph3D/Volund Variants/Standard Character (Specular, Surface)"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_AlphaTex("Alpha", 2D) = "white" {}
		_Overlay("Overlay",2D) = "clear" {}
		_OverlayColor("OverlayColor", Color) = (0,0,0,0)
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}


		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0

		// UI-only data
		[HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
		[HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)

		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0

		// Volund properties
		[HideInInspector] _CullMode ("__cullmode", Float) = 2.0
		[HideInInspector] _SmoothnessInAlbedo ("__smoothnessinalbedo", Float) = 0.0
		_SmoothnessTweak1("Smoothness Scale", Range(0.0, 4.0)) = 1.0
		_SmoothnessTweak2("Smoothness Bias", Range(-1.0, 1.0)) = 0.0
		_SpecularMapColorTweak("Specular Color Tweak", Color) = (1,1,1,1)
		_PlaneReflectionBumpScale("Plane Reflection Bump Scale", Range(0.0, 1.0)) = 0.4
		_PlaneReflectionBumpClamp("Plane Reflection Bump Clamp", Range(0.0, 0.15)) = 0.05
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
		#define USE_SMOOTHNESS_TWEAK
	ENDCG

	SubShader
	{
		Tags {
			//"RenderType"="Opaque"
			"RenderType"="Transparent"
			"Queue"="Transparent"
			"PerformanceChecks"="False"
		}
		LOD 300

			Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			//Blend [_SrcBlend] [_DstBlend]
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite[_ZWrite]
			Cull[_CullMode]

			CGPROGRAM
			#pragma target 3.0
			//#pragma only_renderers d3d11 d3d9 opengl glcore

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _AlphaTex
			#pragma shader_feature _OVERLAY

						// Volund variants
			#pragma shader_feature SMOOTHNESS_IN_ALBEDO

			//We only use the overlay on the base, we don't apply it anywhere else
			#pragma multi_compile OVERLAY_OFF OVERLAY_ON 
			#pragma multi_compile_fwdbase nolightmap
			#pragma multi_compile_fog

			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "Volund_UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			Cull [_CullMode]

			CGPROGRAM
			#pragma target 3.0
			//#pragma only_renderers d3d11 d3d9 opengl glcore

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _OVERLAY

			
			// Volund variants
			#pragma shader_feature SMOOTHNESS_IN_ALBEDO

			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "Volund_UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual
			Cull [_CullMode]

			CGPROGRAM
			#pragma target 3.0
			//#pragma only_renderers d3d11 d3d9 opengl glcore
			
			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
	}
	
	FallBack "VertexLit"
	CustomEditor "VolundMultiStandardShaderGUI"
}
