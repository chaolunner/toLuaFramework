#ifndef EASYRP_INPUT_INCLUDED
#define EASYRP_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame) // UnityPerFrame 缓冲区中的数据在一帧中不会改变。
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw) // UnityPerDraw 缓冲区中的数据在每个物体上不会改变。
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
    // Y分量存有当前物体受多少光源影响的数量。
    float4 unity_LightData;
    real4 unity_LightIndices[2];
    // 反射探针的缓冲数据。
    real4 unity_SpecCube0_HDR;
    real4 unity_SpecCube1_HDR;
    // 指定一个物体展开的uv在光照贴图上的位置。
    float4 unity_LightmapST, unity_DynamicLightmapST;
    // 光照探头是在一个特定点上的简单灯光，编码为球谐函数。
     // 球谐函数在shader中通过七个float4 向量表示，在UnityPerDraw 缓冲区。
    float4 unity_SHAr, unity_SHAg, unity_SHAb;
    float4 unity_SHBr, unity_SHBg, unity_SHBb;
    float4 unity_SHC;
    // Light Probe Proxy Volume 的缓冲数据。
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float3 unity_ProbeVolumeSizeInv;
    float3 unity_ProbeVolumeMin;
    // 阴影探针的缓冲数据（其实就是光照探针，光照探针可以起到阴影探针的作用）。
    float4 unity_ProbesOcclusion;
CBUFFER_END

CBUFFER_START(UnityPerMaterial) // UnityPerMaterial 缓冲区仅在切换材质时改变。
    float4 _MainTex_ST;
    float4 _Color;
    float _Cutoff;
    float _Metallic;
    float _Smoothness;
    float4 _EmissionColor;
CBUFFER_END

CBUFFER_START(UnityPerCamera) // UnityPerCamera 缓冲区可以提供相机位置信息。
    float3 _WorldSpaceCameraPos;
CBUFFER_END

#endif // EASYRP_INPUT_INCLUDED
