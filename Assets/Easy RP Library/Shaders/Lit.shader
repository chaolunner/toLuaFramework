Shader "Easy Render Pipeline/Lit"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo & Alpha", 2D) = "white" {}
		[KeywordEnum(Off, On, Shadows)] _Clipping("Alpha Clipping", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		_Metallic("Metallic", Range(0, 1)) = 0 // 金属光泽
		_Smoothness("Smoothness", Range(0, 1)) = 0.5 // 镜面高光
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2 // 是否双面渲染。
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Pass
		{
			Name "Lit"

			Blend[_SrcBlend][_DstBlend]
			Cull[_Cull]
			ZWrite[_ZWrite]

			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing
			//#pragma instancing_options assumeuniformscaling // 法线的计算需要考虑缩放不一致的情况，所以我们需要去掉这个语法指令。
			#pragma instancing_options lodfade

			#pragma multi_compile _ _SHADOWS_HARD // multi_compile 可以在运行时用EnableKeyword和DisableKeyword设置，并且所有变体都会在编译时加入包中。
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _CASCADED_SHADOWS_HARD _CASCADED_SHADOWS_SOFT

			#pragma shader_feature _CLIPPING_ON // shader_feature 只能在编辑材质球时设置，并且不用的变体不会在编译时加入包中。 
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma shader_feature _PREMULTIPLY_ALPHA

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Lit.hlsl"

			ENDHLSL
		}

		Pass 
		{
			Tags { "LightMode" = "ShadowCaster" }

			Cull [_Cull]

			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling
			#pragma instancing_options lodfade

			#pragma shader_feature _CLIPPING_OFF

			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment

			#include "ShadowCaster.hlsl"

			ENDHLSL
		}
	}

	CustomEditor "UniEasy.Rendering.LitShaderGUI"
}
