#ifndef EASYRP_LIT_INCLUDED
#define EASYRP_LIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame)
	float4x4 unity_MatrixVP;
CBUFFER_END
CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject; 
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
	float4 unity_LightData; // Y分量存有当前物体受多少光源影响的数量。
	real4 unity_LightIndices[2];
CBUFFER_END
// 光源缓冲区
#define MAX_VISIBLE_LIGHTS 16
CBUFFER_START(_LightBuffer)
	float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightSpotDirections[MAX_VISIBLE_LIGHTS];
CBUFFER_END

float3 DiffuseLight(int index, float3 normal, float3 worldPos, float shadowAttenuation) {
	float3 lightColor = _VisibleLightColors[index].rgb;
	float4 lightPositionOrDirection = _VisibleLightDirectionsOrPositions[index];
	float4 lightAttenuation = _VisibleLightAttenuations[index];
	// 当是方向光时，w是0，当是点光源时，w是1，我们利用该性质将 worldPos 与 w 分量相乘，这样就可以用同一个公式计算点光源和方向光的信息。
	float3 lightVector = lightPositionOrDirection.xyz - worldPos * lightPositionOrDirection.w;
	float3 lightDirection = normalize(lightVector);
	float3 spotDirection = _VisibleLightSpotDirections[index].xyz;
	float diffuse = saturate(dot(normal, lightDirection));
	// 和方向光不同，点光源要考虑光源强度随着距离而衰减。这里的衰减关系是距离平方的倒数。为了避免除数是0出现错误，因此加入一个极小的值0.00001。
	float distanceSqr = max(dot(lightVector, lightVector), 0.00001);
	// 点光源还需要考虑光照范围。
	float rangeFade = dot(lightVector, lightVector) * lightAttenuation.x;
	rangeFade = saturate(1.0 - rangeFade * rangeFade);
	rangeFade *= rangeFade;
	// 聚光灯的衰减。
	float spotFade = dot(spotDirection, lightDirection);
	spotFade = saturate(spotFade * lightAttenuation.z + lightAttenuation.w);
	spotFade *= spotFade;

	diffuse *= shadowAttenuation * spotFade * rangeFade / distanceSqr;
	return diffuse * lightColor;
}

// 阴影缓冲区
CBUFFER_START(_ShadowBuffer)
	float4x4 _WorldToShadowMatrices[MAX_VISIBLE_LIGHTS];
	float4 _ShadowData[MAX_VISIBLE_LIGHTS];
	float4 _ShadowMapSize;
CBUFFER_END

TEXTURE2D_SHADOW(_ShadowMap); // 定义阴影纹理
SAMPLER_CMP(sampler_ShadowMap); // 定义阴影采样器状态

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl" // 软阴影采样（SampleShadow_ComputeSamples_Tent_5x5）需要。

float HardShadowAttenuation(float4 shadowPos) 
{
	return SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowPos.xyz);
}

float SoftShadowAttenuation(float4 shadowPos)
{
	real tentWeights[9]; // real不是一个实际的数字类型，而是一个宏，根据需要自动选择float或者half。
	real2 tentUVs[9];
	SampleShadow_ComputeSamples_Tent_5x5(_ShadowMapSize, shadowPos.xy, tentWeights, tentUVs);
	float attenuation = 0;
	for (int i = 0; i < 9; i++)
	{
		attenuation += tentWeights[i] * SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, float3(tentUVs[i].xy, shadowPos.z));
	}
	return attenuation;
}

float ShadowAttenuation(int index, float3 worldPos)
{
	if (_ShadowData[index].x <= 0) {
		return 1.0;
	}
	float4 shadowPos = mul(_WorldToShadowMatrices[index], float4(worldPos, 1.0));
	// 从齐次坐标转换到常规坐标。
	shadowPos.xyz /= shadowPos.w;
	// 通过 SAMPLE_TEXTURE2D_SHADOW 这个宏采样阴影贴图。它需要一张贴图，一个采样器状态，以及对应的阴影空间位置作为参数。
	// 如果该点位置的z值比在阴影贴图中对应点的值要小就会返回1，这说明他比任何投射阴影的物体离光源都要近。
	// 反之，在阴影投射物后面就会返回0。因为采样器会在双线性插值之前先进行比较，所以阴影边缘会混合阴影贴图的多个纹素（texels）。
	float attenuation;
#if defined(_SHADOWS_HARD)
	#if defined(_SHADOWS_SOFT)
		if (_ShadowData[index].y == 0) {
			attenuation = HardShadowAttenuation(shadowPos);
		}
		else
		{
			attenuation = SoftShadowAttenuation(shadowPos);
		}
	#else
		attenuation = HardShadowAttenuation(shadowPos);
	#endif
#else
	attenuation = SoftShadowAttenuation(shadowPos);
#endif

	return lerp(1, attenuation, _ShadowData[index].x);
}

#define UNITY_MATRIX_M unity_ObjectToWorld
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" 

CBUFFER_START(UnityPerMaterial)
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _Color;
CBUFFER_END
#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PreInstance)
#endif

struct VertexInput
{
	float4 pos				: POSITION;
	float2 uv				: TEXCOORD0;
	float3 normal			: NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 clipPos			: SV_POSITION;
	float2 uv				: TEXCOORD0;
	float3 normal			: TEXCOORD1;
	float3 worldPos			: TEXCOORD2;
	float3 vertexLighting	: TEXCOORD3;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput LitPassVertex(VertexInput input)
{
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	output.uv = _MainTex_ST.xy * input.uv + _MainTex_ST.zw;
	output.normal = mul(unity_ObjectToWorld, float4(input.normal, 0));
	//output.normal = mul((float3x3)unity_ObjectToWorld, input.normal); // 如果物体使用统一的scale，可以考虑使用 3X3 模型矩阵简化法线的坐标变换。
	output.worldPos = worldPos.xyz;
	// 由于后四个光源其实并没有那么重要，我们可以将其计算从fragment函数中移到vertex函数中，也就是从逐像素光照改为逐顶点光照，
	// 这样虽然着色的精度会损失一些，但是可以减少GPU的消耗。
	output.vertexLighting = 0;
	for (int i = 4; i < min(unity_LightData.y, 8); i++) { // unity_LightIndices[1] 只能存储4个值。
		int lightIndex = unity_LightIndices[1][i - 4];
		output.vertexLighting += DiffuseLight(lightIndex, input.normal, worldPos.xyz, 1); // 顶点光源现在不会有阴影，所以将阴影衰减值设为1。
	}
	return output;
}

float4 LitPassFragment(VertexOutput input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	float3 col = UNITY_ACCESS_INSTANCED_PROP(PreInstance, _Color).rgb;
	float3 tex = tex2D(_MainTex, input.uv).rgb;
	input.normal = normalize(input.normal); // 坐标变换后在fragment函数中进行归一化。
	float3 diffuseLight = input.vertexLighting;
	for (int i = 0; i < min(unity_LightData.y, 4); i++) { // unity_LightIndices[0] 只能存储4个值。
		int lightIndex = unity_LightIndices[0][i];
		float shadowAttenuation = ShadowAttenuation(lightIndex, input.worldPos);
		diffuseLight += DiffuseLight(lightIndex, input.normal, input.worldPos, shadowAttenuation);
	}
	float3 color = diffuseLight * col * tex;
	return float4(color, 1);
}

#endif // EASYRP_LIT_INCLUDED
