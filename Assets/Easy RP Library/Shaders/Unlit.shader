Shader "Easy Render Pipeline/Unlit"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo & Alpha", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Cull Back
		ZTest LEqual
		ZWrite On

		Pass
		{
			Name "Unlit"

			HLSLPROGRAM

			#pragma target 3.5

			#pragma multi_compile_instancing // 添加对GPU Instancing的支持。
			// 除了 object-to-world 矩阵, 默认 world-to-object 矩阵也存在于Constant Buffer中。
		    // 但是目前我们没有需求使用他们，所以可以通过#pragma instancing_options assumeuniformscaling去掉他们，以提高性能。
			#pragma instancing_options assumeuniformscaling 
			#pragma instancing_options lodfade // 添加对LOD的支持。

			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment

			#include "Unlit.hlsl"

			ENDHLSL
		}
	}
}
