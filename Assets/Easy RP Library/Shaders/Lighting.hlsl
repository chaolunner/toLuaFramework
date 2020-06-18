#ifndef EASYRP_LIGHTING_INCLUDED
#define EASYRP_LIGHTING_INCLUDED

struct LitSurface
{
	float3 normal, position, viewDir; // 法线，顶点位置，摄像机方向。
	float3 diffuse, specular; // 漫反射，镜面反射。
	float perceptualRoughness, roughness, fresnelStrength, reflectivity; // 感知粗糙度，物理粗糙度，菲涅耳反射强度，反射率。
	bool perfectDiffuser; // 是否只使用漫反射。
};

LitSurface GetLitSurface (float3 normal, float3 position, float3 viewDir, float3 color, float metallic, float smoothness, bool perfectDiffuser = false)
{
	LitSurface s;
	s.normal = normal;
	s.position = position;
	s.viewDir = viewDir;
	s.diffuse = color;
	if (perfectDiffuser) {
		s.reflectivity = 0.0;
		smoothness = 0.0;
		s.specular = 0.0;
	}
	else {
		s.specular = lerp(0.04, color, metallic);
		s.reflectivity = lerp(0.04, 1.0, metallic);
		s.diffuse *= 1.0 - s.reflectivity; // 被反射的光也不会色散，因此，除非表面是完美的漫反射，否则应相应地减少色散的颜色。
	}
	s.perfectDiffuser = perfectDiffuser;
	s.perceptualRoughness = 1.0 - smoothness;
	s.roughness = s.perceptualRoughness * s.perceptualRoughness; // 根据迪斯尼模型，此时的粗糙度实际上是一个称为感知粗糙度的值。 物理粗糙度是其平方。
	s.fresnelStrength = saturate(smoothness + s.reflectivity);
	return s;
}

LitSurface GetLitSurfaceVertex (float3 normal, float3 position) 
{
	return GetLitSurface(normal, position, 0, 1, 0, 0, true);
}

// 漫反射，理想的光线，纯白色而无衰减。
float3 LightSurface (LitSurface s, float3 lightDir) 
{
	float3 color = s.diffuse;

	// 使用和 Unity URP 一样的，修改过后的 minimalist CookTorrance BRDF 来计算高光。
	if (!s.perfectDiffuser) {
		float3 halfDir = SafeNormalize(lightDir + s.viewDir);
		float nh = saturate(dot(s.normal, halfDir));
		float lh = saturate(dot(lightDir, halfDir));
		float d = nh * nh * (s.roughness * s.roughness - 1.0) + 1.00001;
		float normalizationTerm = s.roughness * 4.0 + 2.0;
		float specularTerm = s.roughness * s.roughness;
		specularTerm /= (d * d) * max(0.1, lh * lh) * normalizationTerm;
		color += specularTerm * s.specular;
	}

	return color * saturate(dot(s.normal, lightDir));
}

// 根据表面的粗糙度决定反射多少环境。
float3 ReflectEnvironment (LitSurface s, float3 environment) 
{
	if (s.perfectDiffuser) { // 理想的扩散器不会反射任何东西，因此结果始终为零。
		return 0;
	}

	float fresnel = Pow4(1.0 - saturate(dot(s.normal, s.viewDir))); // 菲涅耳反射，实际就是镜面反射随着表面法线和视线方向随着角度的增加而增强。
	environment *= lerp(s.specular, 1, fresnel);
	environment /= s.roughness * s.roughness + 1.0; // 否则，将镜面反射的颜色计入结果中，然后将其除以粗糙度的平方再加一。
	return environment;
}

// 预乘alpha，使玻璃、水等几乎完全透明的材质，仍然可以支持镜面高光。
void PremultiplyAlpha (inout LitSurface s, inout float alpha)
{
	s.diffuse *= alpha;
	alpha = lerp(alpha, 1, s.reflectivity);
}

LitSurface GetLitSurfaceMeta (float3 color, float metallic, float smoothness)
{
	return GetLitSurface(0, 0, 0, color, metallic, smoothness);
}

#endif // EASYRP_LIGHTING_INCLUDED
