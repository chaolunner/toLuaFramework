#ifndef EASYRP_LIT_INCLUDED
#define EASYRP_LIT_INCLUDED

/* ===== ===== ===== ===== ===== START 导入文件 START ===== ===== ===== ===== ===== */

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// 选择适当的反射清晰度，这依赖于 PerceptualRoughnessToMipmapLevel 函数。
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
// 使反射探针支持HDR编码或探测器的强度发生变化，这依赖于 DecodeHDREnvironment 函数。
// 给定UV采样光照贴图，这依赖于 SampleSingleLightmap 函数。
// 为动态物体提供光照探针，这依赖于 SampleSH9 函数。
// 为一个物体提供多个光照探针，这依赖于 SampleProbeVolumeSH4 函数。
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Lighting.hlsl"
// cbuffer 数据。
#include "Input.hlsl"

/* ===== ===== ===== ===== ===== END 导入文件 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 基础数据 START ===== ===== ===== ===== ===== */

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define MAX_VISIBLE_LIGHTS 16 // 最大可见光数量。

/* ===== ===== ===== ===== ===== END 基础数据 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 实时光照 START ===== ===== ===== ===== ===== */

CBUFFER_START(_LightBuffer)
	float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightSpotDirections[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightOcclusionMasks[MAX_VISIBLE_LIGHTS];
CBUFFER_END

float3 GenericLight(int index, LitSurface s, float shadowAttenuation)
{
	float3 lightColor = _VisibleLightColors[index].rgb;
	float4 lightPositionOrDirection = _VisibleLightDirectionsOrPositions[index];
	float4 lightAttenuation = _VisibleLightAttenuations[index];
	// 当是方向光时，w是0，当是点光源时，w是1，我们利用该性质将 s.position 与 w 分量相乘，这样就可以用同一个公式计算点光源和方向光的信息。
	float3 lightVector = lightPositionOrDirection.xyz - s.position * lightPositionOrDirection.w;
	float3 lightDirection = normalize(lightVector);
	float3 spotDirection = _VisibleLightSpotDirections[index].xyz;
	float3 color = LightSurface(s, lightDirection);
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

	color *= shadowAttenuation * spotFade * rangeFade / distanceSqr;
	return color * lightColor;
}

float3 MainLight (LitSurface s, float shadowAttenuation) // LitSurface 来自 Light.hlsl 文件。
{
	float3 lightColor = _VisibleLightColors[0].rgb;
	float3 lightDirection = _VisibleLightDirectionsOrPositions[0].xyz;
	float3 color = LightSurface(s, lightDirection); // 计算漫反射。
	color *= shadowAttenuation;
	return color * lightColor;
}

/* ===== ===== ===== ===== ===== END 实时光照 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 实时阴影 START ===== ===== ===== ===== ===== */

CBUFFER_START(_ShadowBuffer)
	float4x4 _WorldToShadowMatrices[MAX_VISIBLE_LIGHTS];
	float4x4 _WorldToShadowCascadeMatrices[4];
	float4 _CascadeCullingSpheres[4];
	float4 _ShadowData[MAX_VISIBLE_LIGHTS];
	float4 _ShadowMapSize;
	float4 _CascadedShadowMapSize;
	float4 _GlobalShadowData;
	float _CascadedShadowStrength;
	float4 _SubtractiveShadowColor;
CBUFFER_END

TEXTURE2D_SHADOW(_ShadowMap); // 定义阴影纹理
SAMPLER_CMP(sampler_ShadowMap); // 定义阴影采样器状态

TEXTURE2D_SHADOW(_CascadedShadowMap); // 定义主光源级联阴影纹理
SAMPLER_CMP(sampler_CascadedShadowMap); // 定义主光源级联阴影采样器状态

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl" // 软阴影采样（SampleShadow_ComputeSamples_Tent_5x5）需要。

float HardShadowAttenuation(float4 shadowPos, bool cascade = false)
{
	// 通过 SAMPLE_TEXTURE2D_SHADOW 这个宏采样阴影贴图。它需要一张贴图，一个采样器状态，以及对应的阴影空间位置作为参数。
	// 如果该点位置的z值比在阴影贴图中对应点的值要小就会返回1，这说明他比任何投射阴影的物体离光源都要近。
	// 反之，在阴影投射物后面就会返回0。因为采样器会在双线性插值之前先进行比较，所以阴影边缘会混合阴影贴图的多个纹素（texels）。
	if (cascade) {
		return SAMPLE_TEXTURE2D_SHADOW(_CascadedShadowMap, sampler_CascadedShadowMap, shadowPos.xyz);
	}
	else {
		return SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowPos.xyz);
	}
}

float SoftShadowAttenuation(float4 shadowPos, bool cascade = false)
{
	real tentWeights[9]; // real不是一个实际的数字类型，而是一个宏，根据需要自动选择float或者half。
	real2 tentUVs[9];
	float4 size = cascade ? _CascadedShadowMapSize : _ShadowMapSize;
	SampleShadow_ComputeSamples_Tent_5x5(size, shadowPos.xy, tentWeights, tentUVs);
	float attenuation = 0;
	for (int i = 0; i < 9; i++) {
		attenuation += tentWeights[i] * HardShadowAttenuation(float4(tentUVs[i].xy, shadowPos.z, 0), cascade);
	}
	return attenuation;
}

// 基于全局阴影数据计算阴影混合因子的函数。
float RealtimeToBakedShadowsInterpolator(float3 worldPos)
{
	float d = distance(worldPos, _WorldSpaceCameraPos);
	return saturate(d * _GlobalShadowData.y + _GlobalShadowData.z);
}

// 混合实时阴影和烘培阴影的函数。
float MixRealtimeAndBakedShadowAttenuation(float realtime, float4 bakedShadows, int lightIndex, float3 worldPos, bool isMainLight = false)
{
	float t = RealtimeToBakedShadowsInterpolator(worldPos);
	float fadedRealtime = saturate(realtime + t);
	float4 occlusionMask = _VisibleLightOcclusionMasks[lightIndex];
	// 烘培阴影的值越接近灯光的occlusionMaskChannel值，阴影衰减的越厉害（即该灯光可以照亮这块区域）。
	float baked = dot(bakedShadows, occlusionMask);
	bool hasBakedShadows = occlusionMask.x >= 0.0;
#if defined(_SHADOWMASK)
	if (hasBakedShadows) {
		// 使用常规阴影遮罩模式时，只有动态对象投射实时阴影。这样可以消除大量的实时阴影，用阴影贴图和阴影探针替换它们。
		// 虽然渲染成本较低且不限制阴影距离，但渲染质量比使用实时阴影要低。
		fadedRealtime = min(fadedRealtime, baked);
	}
#elif defined(_DISTANCE_SHADOWMASK)
	if (hasBakedShadows) {
		// 在距离阴影遮罩模式下，点光源总是使用烘培阴影。
		bool bakedOnly = _VisibleLightSpotDirections[lightIndex].w > 0.0;
		if (!isMainLight && bakedOnly) {
			fadedRealtime = baked;
		}
		// 距离阴影遮罩模式，在阴影距离内的所有阴影都是实时的，而阴影距离外的则使用烘焙阴影。
		// 因此，这种模式比只使用实时阴影更昂贵，而不是更便宜。
		fadedRealtime = lerp(realtime, baked, t);
	}
#elif defined(_SUBTRACTIVE_LIGHTING)
	#if !defined(LIGHTMAP_ON)
		if (isMainLight) {
			fadedRealtime = min(fadedRealtime, bakedShadows.x);
		}
	#endif
	#if !defined(_CASCADED_SHADOWS_HARD) && !defined(_CASCADED_SHADOWS_SOFT)
		if (lightIndex == 0) {
			fadedRealtime = bakedShadows.x;
		}
	#endif
#endif
	return fadedRealtime;
}

bool SkipRealtimeShadows(float3 worldPos)
{
	// 当该值达到1时，将不再使用实时阴影，因此我们可以跳过采样。
	return RealtimeToBakedShadowsInterpolator(worldPos) >= 1.0;
}

float ShadowAttenuation(int index, float3 worldPos)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#elif !defined(_SHADOWS_HARD) && !defined(_SHADOWS_SOFT)
	return 1.0;
#endif
	if (_ShadowData[index].x <= 0 || SkipRealtimeShadows(worldPos)) {
		return 1.0;
	}
	float4 shadowPos = mul(_WorldToShadowMatrices[index], float4(worldPos, 1.0));
	// 从齐次坐标转换到常规坐标。
	shadowPos.xyz /= shadowPos.w;
	// 在透视除法后对阴影位置的xy坐标做限制，将其限制在0-1范围内，确保阴影采样坐标在tile内。
	shadowPos.xy = saturate(shadowPos.xy);
	shadowPos.xy = shadowPos.xy * _GlobalShadowData.x + _ShadowData[index].zw;
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

// 判断一个点是否在剔除球体内。
float InsideCascadeCullingSphere(int index, float3 worldPos)
{
	float4 s = _CascadeCullingSpheres[index];
	return dot(worldPos - s.xyz, worldPos - s.xyz) < s.w;
}

float CascadedShadowAttenuation(float3 worldPos, bool applyStrength = true)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#elif !defined(_CASCADED_SHADOWS_HARD) && !defined(_CASCADED_SHADOWS_SOFT)
	return 1.0;
#endif
	// 因为剔除球不会与相机和阴影距离对齐，所以级联阴影不会和其他阴影一样在同一距离消失。
	// 我们也一样可以在 CascadedShadowAttenuation 中检查阴影距离来实现统一的效果。
	if (SkipRealtimeShadows(worldPos)) {
		return 1.0;
	}
	// 一点位于一个球的同时，还在更大的球里面。
	// 我们最终可能得到五种情况： (1,1,1,1)，(0,1,1,1)，(0,0,1,1)，(0,0,0,1)，(0,0,0,0)。
	float4 cascadeFlags = float4(
		InsideCascadeCullingSphere(0, worldPos),
		InsideCascadeCullingSphere(1, worldPos),
		InsideCascadeCullingSphere(2, worldPos),
		InsideCascadeCullingSphere(3, worldPos)
	);
	//return dot(cascadeFlags, 0.25); // 可以用来观察级联层次。
	cascadeFlags.yzw = saturate(cascadeFlags.yzw - cascadeFlags.xyz);
	float cascadeIndex = 4 - dot(cascadeFlags, float4(4, 3, 2, 1));
	if (cascadeIndex == 4) { // 在所有级联阴影贴图之外，直接忽略。
		return 1.0;
	}
	float4 shadowPos = mul(_WorldToShadowCascadeMatrices[cascadeIndex], float4(worldPos, 1.0));
	float attenuation;
#if defined(_CASCADED_SHADOWS_HARD)
	attenuation = HardShadowAttenuation(shadowPos, true);
#else
	attenuation = SoftShadowAttenuation(shadowPos, true);
#endif
	if (applyStrength) {
		return lerp(1, attenuation, _CascadedShadowStrength);
	}
	else {
		return attenuation;
	}
}

/* ===== ===== ===== ===== ===== END 实时阴影 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 高光反射 START ===== ===== ===== ===== ===== */

#if !defined(_SRP_BATCHING)
CBUFFER_START(UnityReflectionProbes)
	float4 unity_SpecCube0_BoxMax;
	float4 unity_SpecCube0_BoxMin;
	float4 unity_SpecCube0_ProbePosition;

	float4 unity_SpecCube1_BoxMax;
	float4 unity_SpecCube1_BoxMin;
	float4 unity_SpecCube1_ProbePosition;
CBUFFER_END

TEXTURECUBE(unity_SpecCube1);

// 默认情况下，反射探针的光被视为来自无限远的地方。BoxProjection 可以使小范围内的反射更精确。
float3 BoxProjection(float3 direction, float3 position, float4 cubemapPosition, float4 boxMin, float4 boxMax)
{
	UNITY_BRANCH // 如果if表达式为假，则不执行if中的语句。GLES2和不可识别的平台上被定义为空，则不论表达式的结果是什么，都会执行所有分支的语句。
	if (cubemapPosition.w > 0) {
		float3 factors = ((direction > 0 ? boxMax.xyz : boxMin.xyz) - position) / direction;
		float scalar = min(min(factors.x, factors.y), factors.z);
		direction = direction * scalar + (position - cubemapPosition.xyz);
	}
	return direction;
}
#endif

TEXTURECUBE(unity_SpecCube0); // 获取反射环境，Unity通过unity_SpecCube0在着色器中将其变为可用。这是一个立方体映射贴图资源。
SAMPLER(samplerunity_SpecCube0);

float3 SampleEnvironment(LitSurface s)
{
	float3 reflectVector = reflect(-s.viewDir, s.normal); // 获取反射向量。
	float mip = PerceptualRoughnessToMipmapLevel(s.perceptualRoughness); // 粗糙表面会产生模糊反射，我们可以通过选择适当的 mip level 来获得该模糊反射。
#if !defined(_SRP_BATCHING)
	float3 uvw = BoxProjection(reflectVector, s.position, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax); // 找到调整后的采样坐标。
#else
	float3 uvw = reflectVector;
#endif
	float4 sample = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, uvw, mip); // 采样并确定最终颜色。
	float3 color = DecodeHDREnvironment(sample, unity_SpecCube0_HDR);
#if !defined(_SRP_BATCHING)
	// 混合探针
	float blend = unity_SpecCube0_BoxMin.w;
	if (blend < 0.99999) {
		uvw = BoxProjection(reflectVector, s.position, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
		sample = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube0, uvw, mip);
		color = lerp(DecodeHDREnvironment(sample, unity_SpecCube1_HDR), color, blend);
	}
#endif
	return color;
}

/* ===== ===== ===== ===== ===== END 高光反射 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START GPU INSTANCING START ===== ===== ===== ===== ===== */

// INSTANCING 也可以与 烘培阴影（unity_ProbesOcclusion）一起使用，
// 但是需要我们在导入 UnityInstancing.hlsl 之前，手动定义好 SHADOWS_SHADOWMASK 宏。
#if !defined(LIGHTMAP_ON)
	#if defined(_SHADOWMASK) || defined(_DISTANCE_SHADOWMASK) || defined(_SUBTRACTIVE_LIGHTING)
		#define SHADOWS_SHADOWMASK
	#endif
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" 

#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(PerInstance)
#endif

/* ===== ===== ===== ===== ===== END GPU INSTANCING END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START （顶点/片段）数据结构 START ===== ===== ===== ===== ===== */

struct VertexInput
{
	float4 pos					: POSITION;
	float3 normal				: NORMAL;
	float2 uv					: TEXCOORD0;
	float2 lightmapUV			: TEXCOORD1; // 光照贴图的坐标通过第二个uv通道提供。
	float2 dynamicLightmapUV	: TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	float4 clipPos				: SV_POSITION;
	float3 normal				: TEXCOORD0;
	float3 worldPos				: TEXCOORD1;
	float3 vertexLighting		: TEXCOORD2;
	float2 uv					: TEXCOORD3;
#if defined(LIGHTMAP_ON)	
	float2 lightmapUV			: TEXCOORD4; // 仅在使用光照贴图时启用。
#endif
#if defined(DYNAMICLIGHTMAP_ON)
	float2 dynamicLightmapUV	: TEXCOORD5;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

/* ===== ===== ===== ===== ===== END （顶点/片段）数据结构 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 光照（贴图/探针） START ===== ===== ===== ===== ===== */

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

float3 SampleLightmap (float2 uv) // 采样光照贴图。
{
	return SampleSingleLightmap(
		TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), uv,
		float4(1, 1, 0, 0), // UV坐标的尺度偏移变换，在顶点渲染中做了处理，所以这里提供一个恒等变换。
#if defined(UNITY_LIGHTMAP_FULL_HDR) // 是否需要对光照贴图中的数据进行解码，这依赖于目标平台，如果使用了 HDR 光照贴图，那么解码就不是必须的。
		false,
#else
		true,
#endif
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0) // 提供解码指令，使照明在正确的范围。
	);
}

TEXTURE2D(unity_DynamicLightmap);
SAMPLER(samplerunity_DynamicLightmap);

float3 SampleDynamicLightmap (float2 uv) 
{
	return SampleSingleLightmap(
		TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap), uv,
		float4(1, 1, 0, 0), false,
		float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
	);
}

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

float3 SampleLightProbes (LitSurface s) 
{
	// 如果 unity_ProbeVolumeParams 的x分量被设置，那么就需要使用多个光照探针。
	if (unity_ProbeVolumeParams.x) {
		return SampleProbeVolumeSH4(
			TEXTURE2D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
			s.position, s.normal, unity_ProbeVolumeWorldToObject,
			unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
			unity_ProbeVolumeMin, unity_ProbeVolumeSizeInv
		);
	}
	else {
		float4 coefficients[7];
		coefficients[0] = unity_SHAr;
		coefficients[1] = unity_SHAg;
		coefficients[2] = unity_SHAb;
		coefficients[3] = unity_SHBr;
		coefficients[4] = unity_SHBg;
		coefficients[5] = unity_SHBb;
		coefficients[6] = unity_SHC;
		return max(0.0, SampleSH9(coefficients, s.normal));
	}
}

float3 SubtractiveLighting(LitSurface s, float3 bakedLighting)
{
	float3 lightColor = _VisibleLightColors[0].rgb;
	float3 lightDirection = _VisibleLightDirectionsOrPositions[0].xyz;
	float3 diffuse = lightColor * saturate(dot(lightDirection, s.normal));
	float shadowAttenuation = saturate(CascadedShadowAttenuation(s.position, false) + RealtimeToBakedShadowsInterpolator(s.position));
	float3 shadowedLightingGuess = diffuse * (1.0 - shadowAttenuation);
	float3 subtractedLighting = bakedLighting - shadowedLightingGuess;
	subtractedLighting = max(subtractedLighting, _SubtractiveShadowColor.rgb);
	subtractedLighting = lerp(bakedLighting, subtractedLighting, _CascadedShadowStrength);
	return min(bakedLighting, subtractedLighting);
}

float3 GlobalIllumination (VertexOutput input, LitSurface surface)
{
#if defined(LIGHTMAP_ON)
	float3 gi = SampleLightmap(input.lightmapUV);
	#if defined(_SUBTRACTIVE_LIGHTING)
		gi = SubtractiveLighting(surface, gi);
	#endif
	#if defined(DYNAMICLIGHTMAP_ON)
		gi += SampleDynamicLightmap(input.dynamicLightmapUV);
	#endif
	return gi;
#elif defined(DYNAMICLIGHTMAP_ON)
	return SampleDynamicLightmap(input.dynamicLightmapUV);
#else
	return SampleLightProbes(surface);
#endif
}

/* ===== ===== ===== ===== ===== END 光照（贴图/探针） END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START 烘培阴影 START ===== ===== ===== ===== ===== */

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

float4 BakedShadows (VertexOutput input, LitSurface surface) 
{
	float4 baked = 1.0;
#if defined(LIGHTMAP_ON)
	#if defined(_SHADOWMASK) || defined(_DISTANCE_SHADOWMASK)
		baked = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, input.lightmapUV);
	#endif
#elif defined(_SHADOWMASK) || defined(_DISTANCE_SHADOWMASK) || defined(_SUBTRACTIVE_LIGHTING)
	if (unity_ProbeVolumeParams.x) {
		baked = SampleProbeOcclusion(
			TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
			surface.position, unity_ProbeVolumeWorldToObject,
			unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
			unity_ProbeVolumeMin, unity_ProbeVolumeSizeInv
		);
	}
	baked = unity_ProbesOcclusion;
#endif
	return baked;
}

/* ===== ===== ===== ===== ===== END 烘培阴影 END ===== ===== ===== ===== ===== */

/* ===== ===== ===== ===== ===== START （顶点/片段）着色 START ===== ===== ===== ===== ===== */

VertexOutput LitPassVertex (VertexInput input)
{
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);

#if defined(LIGHTMAP_ON)
	output.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
	output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#if defined(UNITY_ASSUME_UNIFORM_SCALING)
	output.normal = mul((float3x3)UNITY_MATRIX_M, input.normal); // 如果物体使用统一的scale，可以考虑使用 3X3 模型矩阵简化法线的坐标变换。
#else
	output.normal = normalize(mul(input.normal, (float3x3)UNITY_MATRIX_I_M));
#endif
	output.worldPos = worldPos.xyz;
	LitSurface surface = GetLitSurfaceVertex(output.normal, output.worldPos);
	// 由于后四个光源其实并没有那么重要，我们可以将其计算从fragment函数中移到vertex函数中，也就是从逐像素光照改为逐顶点光照，
	// 这样虽然着色的精度会损失一些，但是可以减少GPU的消耗。
	output.vertexLighting = 0;
	for (int i = 4; i < min(unity_LightData.y, 8); i++) { // unity_LightIndices[1] 只能存储4个值。
		int lightIndex = unity_LightIndices[1][i - 4];
		output.vertexLighting += GenericLight(lightIndex, surface, 1); // 顶点光源现在不会有阴影，所以将阴影衰减值设为1。
	}
	return output;
}

float4 LitPassFragment (VertexOutput input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
	albedoAlpha *= UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
	input.normal = normalize(input.normal); // 坐标变换后在fragment函数中进行归一化。
	input.normal = IS_FRONT_VFACE(isFrontFace, input.normal, -input.normal); // 修正只渲染背面时，法线相反的问题。

	float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos.xyz); // 视线方向就是相机位置减去片段位置（归一化）。
	LitSurface surface = GetLitSurface(input.normal, input.worldPos, viewDir, albedoAlpha.rgb, 
		UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Metallic), UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Smoothness));

#if defined(_PREMULTIPLY_ALPHA)
	PremultiplyAlpha(surface, albedoAlpha.a); // 预乘alpha，使玻璃、水等几乎完全透明的材质，仍然可以支持镜面高光。
#endif

	float4 bakedShadows = BakedShadows(input, surface); // 检索烘培阴影。

	float3 color = input.vertexLighting * surface.diffuse; // diffuse == albedoAlpha.rgb 即 _Color + _MainTex 的颜色值。
#if defined(_CASCADED_SHADOWS_HARD) || defined(_CASCADED_SHADOWS_SOFT)
	#if !(defined(LIGHTMAP_ON) && defined(_SUBTRACTIVE_LIGHTING)) // Subtractive 模式下，主灯光将完全烘培，不需要实时光照。
		float shadowAttenuation = MixRealtimeAndBakedShadowAttenuation(CascadedShadowAttenuation(surface.position), bakedShadows, 0, surface.position, true);
		color += MainLight(surface, shadowAttenuation);
	#endif
#endif
	for (int i = 0; i < min(unity_LightData.y, 4); i++) { // unity_LightIndices[0] 只能存储4个值。
		int lightIndex = unity_LightIndices[0][i];
		float shadowAttenuation = MixRealtimeAndBakedShadowAttenuation(ShadowAttenuation(lightIndex, surface.position), bakedShadows, lightIndex, surface.position);
		color += GenericLight(lightIndex, surface, shadowAttenuation);
	}

#if defined(_CLIPPING_ON)
	clip(albedoAlpha.a - _Cutoff); // alpha值小于阈值的片段将被丢弃，不会被渲染。
#endif

	color += ReflectEnvironment(surface, SampleEnvironment(surface));
	color += GlobalIllumination(input, surface);
	color += UNITY_ACCESS_INSTANCED_PROP(PerInstance, _EmissionColor).rgb;

	return float4(color, albedoAlpha.a);
}

/* ===== ===== ===== ===== ===== END （顶点/片段）着色 END ===== ===== ===== ===== ===== */

#endif // EASYRP_LIT_INCLUDED
