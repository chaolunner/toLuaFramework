Shader "Easy Render Pipeline/Lit"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		_Color("Color Tint", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Cull Back
		ZTest LEqual
		ZWrite On

		Pass
		{
			Name "Lit"

			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling 
			#pragma instancing_options lodfade

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Lit.hlsl"

			ENDHLSL
		}
	}
}
