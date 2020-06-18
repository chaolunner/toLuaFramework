#ifndef EASYRP_META_INCLUDED
#define EASYRP_META_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Lighting.hlsl"
#include "Input.hlsl"

CBUFFER_START(UnityMetaPass)
	float unity_OneOverOutputBoost;
	float unity_MaxOutputValue;
	bool4 unity_MetaVertexControl, unity_MetaFragmentControl;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct VertexInput 
{
	float4 pos					: POSITION;
	float2 uv					: TEXCOORD0;
	float2 lightmapUV			: TEXCOORD1;
	float2 dynamicLightmapUV	: TEXCOORD2;
};

struct VertexOutput
{
	float4 clipPos				: SV_POSITION;
	float2 uv					: TEXCOORD0;
};

VertexOutput MetaPassVertex (VertexInput input) 
{
	VertexOutput output;
	// unity_MetaVertexControl 中的x分量表示：烘培的光照贴图，y分量表示：实时的光照贴图。
	if (unity_MetaVertexControl.x) {
		input.pos.xy = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw; // 就像采样光照贴图一样，在渲染光数据时，unity_LightmapST被用来得到正确的映射区域。
	}
	if (unity_MetaVertexControl.y) {
		input.pos.xy = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	}
	input.pos.z = input.pos.z > 0 ? FLT_MIN : 0.0; // 为了让OpenGL烘培光照贴图也能正常运行，我们需要调整z的值，使它不会小于0。
	output.clipPos = mul(unity_MatrixVP, float4(input.pos.xyz, 1.0));
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	return output;
}

float4 MetaPassFragment (VertexOutput input) : SV_TARGET
{
	float4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
	albedoAlpha *= _Color;
	// 透明表面不阻塞灯光，但它们仍然对间接光积累有充分的贡献。结果是全局光照在透明表面附近变得太强烈。
	// 我们可以通过在 meta pass 中用透明度乘以反照率和自发光来弥补这一点。
	albedoAlpha.rgb *= albedoAlpha.a; 
	LitSurface surface = GetLitSurfaceMeta(albedoAlpha.rgb, _Metallic, _Smoothness);
	float4 meta = 0;
	// meta pass 也用于生成其他数据。通过 unity_MetaFragmentControl 中的x分量，判断是否应该输出反照率（albedo）。
	if (unity_MetaFragmentControl.x) {
		meta = float4(surface.diffuse, 1);
		// 高光材质也会提供一些间接光（indirect light）。
		meta.rgb += surface.specular * surface.roughness * 0.5;
		// 通过 unity_OneOverOutputBoost 提供的指数以及定义最大亮度的 unity_MaxOutputValue 来调整反照率（albedo）强度。
		// 并将其限制在零和最大值之间。
		meta.rgb = clamp(PositivePow(meta.rgb, unity_OneOverOutputBoost), 0, unity_MaxOutputValue);
	}
	// meta pass 也用于收集从表面发出的光线。当在 LitShaderGUI.OnGUI 中调用 editor.LightmapEmissionPropertry()，
	// 提供了一个全局照明属性后，unity_MetaFragmentControl 中的y分量就会被设置。
	if (unity_MetaFragmentControl.y) {
		meta = float4(_EmissionColor.rgb * albedoAlpha.a, 1);
	}
	return meta;
}

#endif // EASYRP_META_INCLUDED
