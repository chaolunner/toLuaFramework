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
	// 未使用，但SRP Batcher需要确保各个Pass之间UnityPerDraw buffer保持不变。
	float4 unity_LightData; 
	real4 unity_LightIndices[2];
CBUFFER_END
CBUFFER_START(_ShadowCasterBuffer)
	float _ShadowBias;
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" 

struct VertexInput
{
	float4 pos			: POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 clipPos		: SV_POSITION;
};

VertexOutput ShadowCasterPassVertex(VertexInput input) {
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
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
	return output;
}

float4 ShadowCasterPassFragment(VertexOutput input) : SV_TARGET{
	return 0;
}

#endif // EASYRP_SHADOWCASTER_INCLUDED
