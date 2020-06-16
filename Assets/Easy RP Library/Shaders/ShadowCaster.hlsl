#ifndef EASYRP_SHADOWCASTER_INCLUDED
#define EASYRP_SHADOWCASTER_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame)
	float4x4 unity_MatrixVP;
CBUFFER_END
CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject; 
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
	// 未使用，但SRP Batching需要确保各个Pass之间UnityPerDraw buffer保持不变。
	float4 unity_LightData; 
	real4 unity_LightIndices[2];
	float4 unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax;
	float4 unity_SpecCube0_ProbePosition, unity_SpecCube0_HDR;
	float4 unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax;
	float4 unity_SpecCube1_ProbePosition, unity_SpecCube1_HDR;
CBUFFER_END
CBUFFER_START(_ShadowCasterBuffer)
	float _ShadowBias;
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" 

CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;
	float4 _Color;
	float _Cutoff;
CBUFFER_END
#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)
#endif

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct VertexInput
{
	float4 pos			: POSITION;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 clipPos		: SV_POSITION;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput ShadowCasterPassVertex (VertexInput input)
{
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	// 因为摄像机 近平面 的存在，本因形成遮挡的顶点可能被忽略，为了避免这种情况，我们在顶点函数中，限制顶点不超出近平面。
	// 根据投影空间z值从齐次坐标转换为正常坐标后的范围为 -1 到 1，可以通过 z，w 限制。
	// 但是，OpenGL之外的 API，近平面 z值 = 1，而 OpenGL 近平面 z值 = -1，所以我们需要通过 UNITY_REVERSED_Z 和 UNITY_NEAR_CLIP_VALUE 这两个宏覆盖所有情况。
#if UNITY_REVERSED_Z
	output.clipPos.z -= _ShadowBias;
	output.clipPos.z = min(output.clipPos.z, output.clipPos.w * UNITY_NEAR_CLIP_VALUE);
#else
	output.clipPos.z += _ShadowBias;
	output.clipPos.z = max(output.clipPos.z, output.clipPos.w * UNITY_NEAR_CLIP_VALUE);
#endif
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	return output;
}

float4 ShadowCasterPassFragment (VertexOutput input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
#if !defined(_CLIPPING_OFF)
	float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
	alpha *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).a;
	clip(alpha - _Cutoff);
#endif
	return 0;
}

#endif // EASYRP_SHADOWCASTER_INCLUDED
