#ifndef EASYRP_UNLIT_INCLUDED // 使用 #ifndef 来避免多次引用
#define EASYRP_UNLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl" // 使用 CBUFFER_START 和 CBUFFER_END 这两个宏需要

// Unity 没有直接提供MVP矩阵，而是拆分成两个矩阵M和VP，因为VP矩阵在一帧中不会改变，可以重复利用。
// Unity 将M矩阵和VP矩阵存入Constant Buffer中以提高运算效率。
// 因为Constant Buffer并不是支持所有平台，所以我们使用宏来代替直接使用cbuffer keyword。
CBUFFER_START(UnityPerFrame) // VP矩阵：存入的buffer为UnityPerFrame，也就是每一帧VP矩阵不会改变。
	float4x4 unity_MatrixVP;
CBUFFER_END
CBUFFER_START(UnityPerDraw) // M矩阵：存入的buffer为UnityPerDraw, 也就是针对每个物体的绘制不会改变。
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject; // 后面这三个即使不用也需要加，不然会破坏兼容性，导致 Builtin property offset in cbuffer overlap other stages (UnityPerDraw) 错误。
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
CBUFFER_END

// 当Instancing开启时，GPU会使用相同的Constant Data渲染同一个mesh多次。
// 但是因为每个物体的位置不同，所以M矩阵就不同。
// 为了解决这个问题，会在Constant Buffer中存入一个数组，用于存储待渲染的物体的M矩阵。
// 每一个instance根据自身的index，从数组中取用数据。
// 我们使用unity提供的UNITY_MATRIX_M这个宏，在使用矩阵数组的时候可以取出对应的矩阵。
#define UNITY_MATRIX_M unity_ObjectToWorld
// 使用 UNITY_MATRIX_M 、UNITY_VERTEX_INPUT_INSTANCE_ID 和 UNITY_SETUP_INSTANCE_ID 等宏的需要。
// 注意，不要在 UNITY_MATRIX_M 宏之前引入，会导致启用Instancing的物体位置不对。
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" 

CBUFFER_START(UnityPerMaterial) // UnityPerMaterial缓冲区仅在切换材质时改变。
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _Color;
CBUFFER_END
// 注意，UNITY_INSTANCING_ENABLED 宏必须要加，因为 SRP Batch 需要将所有属性都加入到 UnityPerMaterial buffer 中，
// 而 Instancing 又需要将 _Color 属性加入到 PreInstance buffer 中，两者存在冲突，同时开启会出错，
// 但 UNITY_INSTANCING_ENABLED 可以在 SRP Batch 运行时，返回 false，这样我们就可以跳过下面的步骤了。
#if defined(UNITY_INSTANCING_ENABLED)	
UNITY_INSTANCING_BUFFER_START(PreInstance) // 当用Instancing时，将color属性存入Constant Buffer，使一个material渲染多种颜色，并且可以合并draw call。
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PreInstance)
#endif

struct VertexInput
{
	float4 pos			: POSITION;
	float2 uv			: TEXCOORD0;
	// 当用Instancing时，物体的index会被gpu传入顶点数据中，UNITY_MATRIX_M 这个宏需要使用这个index数据。
	// 所以我们需要将它加入到VertexInput结构体中，使用 UNITY_VERTEX_INPUT_INSTANCE_ID 这个宏来使其生效。
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 clipPos		: SV_POSITION;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput UnlitPassVertex(VertexInput input)
{
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input); // 最后，我们必须在UnlitPassVertex方法使用UNITY_MATRIX_M之前，调用 UNITY_SETUP_INSTANCE_ID 宏，使index可用。
	UNITY_TRANSFER_INSTANCE_ID(input, output); // 因为需要在UnlitPassFragment方法中调用color属性，所以需要将index从input复制到output（同时需要在 VertexOutput 结构体中添加 UNITY_VERTEX_INPUT_INSTANCE_ID），为此可以使用 UNITY_TRANSFER_INSTANCE_ID 宏。
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	output.uv = _MainTex_ST.xy * input.uv + _MainTex_ST.zw;
	return output;
}

float4 UnlitPassFragment(VertexOutput input) : SV_Target
{
	//float3 tex = tex2D(_MainTex, input.uv).rgb;
	//float3 color = _Color * tex;
	//return float4(color, 1);
	UNITY_SETUP_INSTANCE_ID(input);
	float4 col = UNITY_ACCESS_INSTANCED_PROP(PreInstance, _Color); // 根据index取用color。
	float3 tex = tex2D(_MainTex, input.uv).rgb;
	float3 color = col * tex;
	return float4(color, 1);
}

#endif // EASYRP_UNLIT_INCLUDED
