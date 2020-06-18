using UnityEngine;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Experimental.GlobalIllumination;
using Conditional = System.Diagnostics.ConditionalAttribute; // 与 UnityEngine.Debug 类冲突，所以取个别名。
using LightType = UnityEngine.LightType; // 与 UnityEngine.Experimental.GlobalIllumination 类冲突，取别名。

namespace UniEasy.Rendering
{
    public class EasyPipeline : RenderPipeline
    {
        public bool UseDynamicBatching;
        public bool UseInstancing;
        private CommandBuffer cameraBuffer = new CommandBuffer()
        {
            name = cameraBufferName
        };
        private CullingResults cullingResults;
        private const string cameraBufferName = "Render Camera";
        private const string SRPBatchingKeyword = "_SRP_BATCHING";
        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId[] legacyShaderTagIds = {
                                                            new ShaderTagId("Always"),
                                                            new ShaderTagId("ForwardBase"),
                                                            new ShaderTagId("PrepassBase"),
                                                            new ShaderTagId("Vertex"),
                                                            new ShaderTagId("VertexLMRGBM"),
                                                            new ShaderTagId("VertexLM")
                                                          };
        private static Material errorMaterial;
        private string SampleName { get; set; }

        // 实时光照。
        private Vector4[] visibleLightColors = new Vector4[maxVisibleLights];
        private Vector4[] visibleLightDirectionsOrPositions = new Vector4[maxVisibleLights];
        private Vector4[] visibleLightAttenuations = new Vector4[maxVisibleLights];
        private Vector4[] visibleLightSpotDirections = new Vector4[maxVisibleLights];
        private bool mainLightExists; // 主光源是否存在。
        private const int maxVisibleLights = 16;
        private static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
        private static int visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
        private static int visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
        private static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");
        private static int unity_LightDataID = Shader.PropertyToID("unity_LightData");

        // 实时阴影。
        public int ShadowMapSize;
        public int ShadowCascades;
        public float ShadowDistance;
        public Vector3 ShadowCascadeSplit;
        private RenderTexture shadowMap, cascadedShadowMap;
        private CommandBuffer shadowBuffer = new CommandBuffer
        {
            name = shadowBufferName
        };
        private Vector4[] shadowData = new Vector4[maxVisibleLights];
        private Vector4[] cascadeCullingSpheres = new Vector4[4];
        private Matrix4x4[] worldToShadowMatrices = new Matrix4x4[maxVisibleLights];
        private Matrix4x4[] worldToShadowCascadeMatrices = new Matrix4x4[4];
        private int shadowTileCount;
        private const string shadowBufferName = "Render Shadows";
        private const string shadowsHardKeyword = "_SHADOWS_HARD";
        private const string shadowsSoftKeyword = "_SHADOWS_SOFT";
        private const string cascadedShadowsHardKeyword = "_CASCADED_SHADOWS_HARD";
        private const string cascadedShadowsSoftKeyword = "_CASCADED_SHADOWS_SOFT";
        private static int worldToShadowMatricesId = Shader.PropertyToID("_WorldToShadowMatrices");
        private static int shadowMapId = Shader.PropertyToID("_ShadowMap");
        private static int shadowBiasId = Shader.PropertyToID("_ShadowBias");
        private static int shadowDataId = Shader.PropertyToID("_ShadowData");
        private static int shadowMapSizeId = Shader.PropertyToID("_ShadowMapSize");
        private static int globalShadowDataId = Shader.PropertyToID("_GlobalShadowData");
        private static int cascadedShadowMapId = Shader.PropertyToID("_CascadedShadowMap");
        private static int worldToShadowCascadeMatricesId = Shader.PropertyToID("_WorldToShadowCascadeMatrices");
        private static int cascadedShadowMapSizeId = Shader.PropertyToID("_CascadedShadowMapSize");
        private static int cascadedShadoStrengthId = Shader.PropertyToID("_CascadedShadowStrength");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");

        // 光照贴图
#if UNITY_EDITOR
        // Unity 默认使用传统管线的光线衰减方式来烘培光照，而我们所使用的是物理正确的平方反比衰减（physically-correct inverse squared falloff），
        // 这对于定向光来说这不是问题，因为它们没有衰减。但对于点光源或者聚光源就会出现光贡献太多的问题，所以我们需要告诉 Unity 使用哪个衰减函数。
        private static Lightmapping.RequestLightsDelegate lightmappingLightsDelegate = 
            (Light[] inputLights, NativeArray<LightDataGI> outputLights) => {
                // 我们必须遍历所有灯光，适当地配置LightDataGI结构，将其衰减设置为FalloffType.InverseSquared，然后将其复制到输出数组。
                LightDataGI lightData = new LightDataGI();
                for (int i = 0; i < inputLights.Length; i++)
                {
                    Light light = inputLights[i];
                    switch (light.type)
                    {
                        case LightType.Directional:
                            var directionalLight = new DirectionalLight();
                            LightmapperUtils.Extract(light, ref directionalLight);
                            lightData.Init(ref directionalLight);
                            break;
                        case LightType.Point:
                            var pointLight = new PointLight();
                            LightmapperUtils.Extract(light, ref pointLight);
                            lightData.Init(ref pointLight);
                            break;
                        case LightType.Spot:
                            var spotLight = new SpotLight();
                            LightmapperUtils.Extract(light, ref spotLight);
                            lightData.Init(ref spotLight);
                            break;
                        case LightType.Area:
                            var rectangleLight = new RectangleLight();
                            LightmapperUtils.Extract(light, ref rectangleLight);
                            lightData.Init(ref rectangleLight);
                            break;
                        default:
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                    }
                    lightData.falloff = FalloffType.InverseSquared;
                    outputLights[i] = lightData;
                }
            };
#endif

        public EasyPipeline(bool useSRPBatching)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
#if UNITY_EDITOR
            Lightmapping.SetDelegate(lightmappingLightsDelegate);
#endif
        }

#if UNITY_EDITOR
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Lightmapping.ResetDelegate();
        }
#endif

        // 这个函数会在绘制管线时调用，两个参数，第一个为所有渲染相关的内容（不光只有
        // 渲染目标，同时还有灯光，反射探针，光照探针等等相关的东西），第二个为相机组。
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                PrepareBuffer(cameras[i]);
#if UNITY_EDITOR
                PrepareForSceneWindow(cameras[i]);
#endif
                if (!Cull(context, cameras[i])) { return; }
                if (cullingResults.visibleLights.Length > 0)
                {
                    ConfigureLights();
                    if (mainLightExists)
                    {
                        RenderCascadedShadows(context);
                    }
                    else
                    {
                        cameraBuffer.DisableShaderKeyword(cascadedShadowsHardKeyword);
                        cameraBuffer.DisableShaderKeyword(cascadedShadowsSoftKeyword);
                    }
                    if (shadowTileCount > 0)
                    {
                        RenderShadows(context);
                    }
                    else
                    {
                        cameraBuffer.DisableShaderKeyword(shadowsHardKeyword);
                        cameraBuffer.DisableShaderKeyword(shadowsSoftKeyword);
                    }
                }
                else
                {
                    // 当没有光源时，清除关于光源数量的缓存。
                    cameraBuffer.SetGlobalVector(unity_LightDataID, Vector4.zero);
                    cameraBuffer.DisableShaderKeyword(shadowsHardKeyword);
                    cameraBuffer.DisableShaderKeyword(shadowsSoftKeyword);
                    cameraBuffer.DisableShaderKeyword(cascadedShadowsHardKeyword);
                    cameraBuffer.DisableShaderKeyword(cascadedShadowsSoftKeyword);
                }
                Setup(context, cameras[i]);
                Draws(context, cameras[i]);
                DrawUnsupportedShaders(context, cameras[i]);
#if UNITY_EDITOR
                DrawGizmos(context, cameras[i]);
#endif
                Submit(context, cameras[i]);

                if (shadowMap)
                {
                    RenderTexture.ReleaseTemporary(shadowMap);
                    shadowMap = null;
                }
                if (cascadedShadowMap)
                {
                    RenderTexture.ReleaseTemporary(cascadedShadowMap);
                    cascadedShadowMap = null;
                }
            }
        }

        private void PrepareBuffer(Camera camera)
        {
            Profiler.BeginSample("Editor Only");
            cameraBuffer.name = SampleName = camera.name;
            Profiler.EndSample();
        }

        private void PrepareForSceneWindow(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }

        private bool Cull(ScriptableRenderContext context, Camera camera)
        {
            // 剪裁，这边应该是相机的视锥剪裁相关。
            // 自定义一个剪裁参数，ScriptableCullingParameters类里有很多可以设置的东西。我们先采用相机的默认剪裁参数。
            // 直接使用相机默认剪裁参数。
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
            {
                // 在 Cull 之前设置阴影距离。
                cullParam.shadowDistance = Mathf.Min(ShadowDistance, camera.farClipPlane);
                // 获取剪裁之后的全部结果（其中不仅有渲染物体，还有相关的其他渲染要素）。
                cullingResults = context.Cull(ref cullParam);
                return true;
            }
            return false;
        }

        private void ConfigureLights()
        {
            mainLightExists = false;
            shadowTileCount = 0;
            for (int i = 0; i < cullingResults.visibleLights.Length; i++)
            {
                if (i == maxVisibleLights) { break; }
                VisibleLight light = cullingResults.visibleLights[i];
                // finalColor字段存储了光源的颜色，该颜色数据是由光源的color属性和intensity属性相乘后的结果，并经过了颜色空间的校正。
                visibleLightColors[i] = light.finalColor;
                Vector4 attenuation = Vector4.zero;
                attenuation.w = 1f; // 为了保证不同类型的光照计算的一致性（用同样的shader代码），将w分量设置为1。
                Vector4 shadow = Vector4.zero;
                if (light.lightType == LightType.Directional)
                {
                    // 方向光的光源方向信息可以通过光源的旋转信息获得，光源的方向是它的z轴方向。
                    // 我们可以通过VisibleLight.localToWorldMatrix矩阵获取在世界坐标系中的该信息。这个矩阵的第三列定义了光源的本地Z轴方向。
                    Vector4 v = light.localToWorldMatrix.GetColumn(2);
                    // 在shader中我们使用从物体朝向光源的向量方向进行计算，所以将获得的光源方向进行取反操作。
                    v.x = -v.x;
                    v.y = -v.y;
                    v.z = -v.z;
                    visibleLightDirectionsOrPositions[i] = v;
                    // 设置阴影。
                    shadow = ConfigureShadows(i, light.light);
                    shadow.z = 1f; // 用阴影数据的z分量来区分是方向光还是聚光灯。
                    if (i == 0 && shadow.x > 0f && ShadowCascades > 0)
                    {
                        mainLightExists = true;
                        // 我们会为主光源提供单独的渲染贴图，所以当我们拥有主光源时，让图块计数减1，
                        // 并且在RenderShadows函数中将其从常规阴影贴图渲染中排除。
                        shadowTileCount -= 1;
                    }
                }
                else if (light.lightType == LightType.Point || light.lightType == LightType.Spot)
                {
                    // 点光源不关心光的方向而关心光源的位置。这个矩阵的第四列定义了光源的世界位置。
                    visibleLightDirectionsOrPositions[i] = light.localToWorldMatrix.GetColumn(3);
                    attenuation.x = 1f / Mathf.Max(light.range * light.range, 0.00001f);
                    if (light.lightType == LightType.Spot)
                    {
                        // 聚光灯的光源方向。
                        Vector4 v = light.localToWorldMatrix.GetColumn(2);
                        v.x = -v.x;
                        v.y = -v.y;
                        v.z = -v.z;
                        visibleLightSpotDirections[i] = v;
                        // 聚光灯的衰减。
                        float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
                        float outerCos = Mathf.Cos(outerRad);
                        float outerTan = Mathf.Tan(outerRad);
                        float innerCos = Mathf.Cos(Mathf.Atan(((46f / 64f) * outerTan)));
                        float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
                        attenuation.z = 1f / angleRange;
                        attenuation.w = -outerCos * attenuation.z;
                        // 设置阴影。
                        shadow = ConfigureShadows(i, light.light);
                    }
                }
                // 填充点光源或聚光灯的光照范围。
                visibleLightAttenuations[i] = attenuation;
                shadowData[i] = shadow;
            }

            cameraBuffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
            cameraBuffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
            cameraBuffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations); // 点光源。
            cameraBuffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections); // 聚光灯。

            // 尽管目前我们已经支持到场景中最多16个光源，但是依然无法避免有可能会存在更多光源的情况。
            // 当超出时，我们需要告诉Unity需要将一些光源舍弃以避免数组的越界。
            // 如果主光源存在，在渲染前将主光源移出可见光列表，以防止在shader中计算多次（会导致像素光数量上限变成5个）。
            if (mainLightExists || cullingResults.visibleLights.Length > maxVisibleLights)
            {
                var lightIndexs = cullingResults.GetLightIndexMap(Unity.Collections.Allocator.TempJob);
                if (mainLightExists) { lightIndexs[0] = -1; }
                for (int i = maxVisibleLights; i < cullingResults.visibleLights.Length; i++)
                {
                    lightIndexs[i] = -1;
                }
                cullingResults.SetLightIndexMap(lightIndexs);
                lightIndexs.Dispose();
            }
        }

        private Vector4 ConfigureShadows(int lightIndex, Light shadowLight)
        {
            Vector4 shadow = Vector4.zero;
            Bounds shadowBounds;
            if (shadowLight.shadows != LightShadows.None && cullingResults.GetShadowCasterBounds(lightIndex, out shadowBounds))
            {
                shadowTileCount += 1;
                shadow.x = shadowLight.shadowStrength;
                shadow.y = shadowLight.shadows == LightShadows.Soft ? 1f : 0f;
            }
            return shadow;
        }

        private void RenderShadows(ScriptableRenderContext context)
        {
            // 设置阴影贴图图集大小。
            int split = 4;
            if (shadowTileCount <= 1) { split = 1; }
            else if (shadowTileCount <= 4) { split = 2; }
            else if (shadowTileCount <= 9) { split = 3; }
            float tileSize = ShadowMapSize / split;
            float tileScale = 1f / split;
            Rect tileViewport = new Rect(0f, 0f, tileSize, tileSize);

            shadowMap = SetShadowRenderTarget(); // 设置阴影渲染目标。

            shadowBuffer.BeginSample(shadowBufferName);
            shadowBuffer.SetGlobalVector(globalShadowDataId, new Vector4(tileScale, ShadowDistance * ShadowDistance));
            context.ExecuteCommandBuffer(shadowBuffer);
            shadowBuffer.Clear();
            int tileIndex = 0;
            bool hardShadows = false;
            bool softShadows = false;
            for (int i = mainLightExists ? 1 : 0; i < cullingResults.visibleLights.Length; i++)
            {
                if (i == maxVisibleLights)
                {
                    break;
                }
                if (shadowData[i].x <= 0f)
                {
                    continue;
                }
                // 聚光灯的视角投影矩阵。我们可以通过 CullingResults 中的 ComputeSpotShadowMatricesAndCullingPrimitives 方法得到。
                // 该方法的第一个参数是光源序列，视野矩阵 和 投影矩阵 则是在后两个输出参数中。
                // 最后一个参数 阴影剔除数据 我们用不到，但作为输出参数，我们必须提供。
                // 方向光的视角投影矩阵。我们可以通过 CullingResults 中的 ComputeDirectionalShadowMatricesAndCullingPrimitives 方法得到。
                // 该方法的第一个参数是光源序列，接着是级联序列，级联数量，级联分级的三维向量，整型的图块尺寸，阴影近平面值，视野矩阵，投影矩阵 和 阴影剔除数据。
                // 阴影剔除数据中包含了一个有效的剔除球体。该球体包裹了所有需要被渲染进直射光阴影贴图的物体。
                // 这对于方向光来说非常重要，因为方向光不像聚光灯，它会影响所有物体，我们需要有一个剔除球体来限制渲染进阴影贴图的图形数量。
                Matrix4x4 viewMatrix, projectionMatrix;
                ShadowSplitData splitData;
                bool validShadows;
                if (shadowData[i].z > 0f)
                {
                    validShadows = cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(i, 0, 1, Vector3.right, (int)tileSize,
                            cullingResults.visibleLights[i].light.shadowNearPlane,
                            out viewMatrix, out projectionMatrix, out splitData);
                }
                else
                {
                    validShadows = cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(i, out viewMatrix, out projectionMatrix, out splitData);
                }
                if (!validShadows)
                {
                    shadowData[i].x = 0f;
                    continue;
                }
                if (shadowData[i].y <= 0f) { hardShadows = true; }
                else { softShadows = true; }
                // 设置阴影tiles，计算tiles偏移，设置视口以及剪裁，返回偏移值。
                Vector2 tileOffset = ConfigureShadowTile(tileIndex, split, tileSize);
                shadowData[i].z = tileOffset.x * tileSize;
                shadowData[i].w = tileOffset.y * tileSize;
                // 当我们获得了该矩阵，调用 shadowBuffer 的 SetViewProjectionMatrices 方法，然后执行 CommandBuffer 并清理。 
                shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                // 在渲染深度贴图（ShadowCaster）时，在深度上添加一点偏移，来掩盖阴影的瑕疵（条纹状阴影）。
                shadowBuffer.SetGlobalFloat(shadowBiasId, cullingResults.visibleLights[i].light.shadowBias);
                context.ExecuteCommandBuffer(shadowBuffer);
                shadowBuffer.Clear();

                // 有了正确的矩阵信息，我们现在可以渲染所有投射阴影的物体了。
                // 我们通过调用 DrawShadows 方法来实现。 这个方法需要一个 DrawShadowsSettings 类型的引用参数。
                // 我们用 cullingResults 和 光源序列 作为参数来创建一个该实例。
                var shadowSettings = new ShadowDrawingSettings(cullingResults, i);
                context.DrawShadows(ref shadowSettings);
                // 计算world-to-shadow矩阵。
                CalculateWorldToShadowMatrix(ref viewMatrix, ref projectionMatrix, out worldToShadowMatrices[i]);

                tileIndex += 1;
            }
            shadowBuffer.DisableScissorRect(); // 我们在渲染阴影后调用 DisableScissorRect 关闭裁剪矩形，不然会影响到后面的常规渲染。
            shadowBuffer.SetGlobalTexture(shadowMapId, shadowMap);
            shadowBuffer.SetGlobalVectorArray(shadowDataId, shadowData);
            shadowBuffer.SetGlobalMatrixArray(worldToShadowMatricesId, worldToShadowMatrices);

            CoreUtils.SetKeyword(shadowBuffer, shadowsHardKeyword, hardShadows);
            CoreUtils.SetKeyword(shadowBuffer, shadowsSoftKeyword, softShadows);

            float invShadowMapSize = 1f / ShadowMapSize;
            shadowBuffer.SetGlobalVector(shadowMapSizeId, new Vector4(invShadowMapSize, invShadowMapSize, ShadowMapSize, ShadowMapSize));

            shadowBuffer.EndSample(shadowBufferName);
            context.ExecuteCommandBuffer(shadowBuffer);
            shadowBuffer.Clear();
        }

        private RenderTexture SetShadowRenderTarget()
        {
            RenderTexture texture = RenderTexture.GetTemporary(ShadowMapSize, ShadowMapSize, 16, RenderTextureFormat.Shadowmap);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // 告诉GPU将渲染信息写入阴影贴图中。
            // 我们会清理这个纹理，所以我们并不关注它来自哪，可以用 RenderBufferLoadAction.DontCare 来指明这一点，这将使得tile-based架构的GPU有更高的执行效率。
            // 我们随后需要采样该纹理，所以需要将其存储在内存中，通过 RenderBufferStoreAction.Store 来指明这一点。
            // 我们只关注深度通道，通过 ClearFlag.Depth 来指明这一点。
            CoreUtils.SetRenderTarget(shadowBuffer, texture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Depth);
            return texture;
        }

        private Vector2 ConfigureShadowTile(int tileIndex, int split, float tileSize)
        {
            Vector2 tileOffset;
            // 设置适当的阴影图集区域。
            tileOffset.x = tileIndex % split;
            tileOffset.y = tileIndex / split;
            var tileViewport = new Rect(tileOffset.x * tileSize, tileOffset.y * tileSize, tileSize, tileSize);
            shadowBuffer.SetViewport(tileViewport);
            // 给阴影图集之间保留一点间距，防止采样时出错。
            shadowBuffer.EnableScissorRect(new Rect(
                tileViewport.x + 4f, tileViewport.y + 4f,
                tileSize - 8f, tileSize - 8f
            ));
            return tileOffset;
        }

        private void CalculateWorldToShadowMatrix(ref Matrix4x4 viewMatrix, ref Matrix4x4 projectionMatrix, out Matrix4x4 worldToShadowMatrix)
        {
            // OpenGL API 裁减空间 z轴相反，为了应对所有情况，我们需要取反，即修改所有2号序列。
            if (SystemInfo.usesReversedZBuffer)
            {
                projectionMatrix.m20 = -projectionMatrix.m20;
                projectionMatrix.m21 = -projectionMatrix.m21;
                projectionMatrix.m22 = -projectionMatrix.m22;
                projectionMatrix.m23 = -projectionMatrix.m23;
            }
            // 我们现在有了世界空间至阴影空间的转换矩阵。裁减空间范围是-1到1，但我们的纹理坐标和深度范围在0到1。
            // 要映射至该范围就得再额外乘一个能在所有维度缩放和偏移0.5个单位的转换矩阵。我们可以用Matrix4x4.TRS方法来得到想要的缩放、旋转或偏移。
            // var scaleOffset = Matrix4x4.TRS(Vector3.one * 0.5f, Quaternion.identity, Vector3.one * 0.5f);
            // 但是其实这是一个简单的矩阵，我们在单位矩阵的基础上修改合适的分量即可。
            var scaleOffset = Matrix4x4.identity;
            scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
            scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
            worldToShadowMatrix = scaleOffset * (projectionMatrix * viewMatrix);
        }

        private void RenderCascadedShadows(ScriptableRenderContext context)
        {
            float tileSize = ShadowMapSize / 2;
            cascadedShadowMap = SetShadowRenderTarget();
            shadowBuffer.BeginSample(shadowBufferName);
            shadowBuffer.SetGlobalVector(globalShadowDataId, new Vector4(0, ShadowDistance * ShadowDistance));
            context.ExecuteCommandBuffer(shadowBuffer);
            shadowBuffer.Clear();
            Light shadowLight = cullingResults.visibleLights[0].light;
            shadowBuffer.SetGlobalFloat(shadowBiasId, shadowLight.shadowBias);
            var shadowSettings = new ShadowDrawingSettings(cullingResults, 0);
            var tileMatrix = Matrix4x4.identity;
            tileMatrix.m00 = tileMatrix.m11 = 0.5f;

            for (int i = 0; i < ShadowCascades; i++)
            {
                Matrix4x4 viewMatrix, projectionMatrix;
                ShadowSplitData splitData;
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    0, i, ShadowCascades, ShadowCascadeSplit, (int)tileSize,
                    shadowLight.shadowNearPlane,
                    out viewMatrix, out projectionMatrix, out splitData
                );

                Vector2 tileOffset = ConfigureShadowTile(i, 2, tileSize);
                shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                context.ExecuteCommandBuffer(shadowBuffer);
                shadowBuffer.Clear();

                shadowSettings.splitData = splitData;
                cascadeCullingSpheres[i] = splitData.cullingSphere; // xyz分量描述球的位置，w分量定义球的半径。
                cascadeCullingSpheres[i].w *= splitData.cullingSphere.w;
                Vector3 pos = new Vector3(splitData.cullingSphere.x, splitData.cullingSphere.y, splitData.cullingSphere.z);
                context.DrawShadows(ref shadowSettings);
                CalculateWorldToShadowMatrix(ref viewMatrix, ref projectionMatrix, out worldToShadowCascadeMatrices[i]);
                tileMatrix.m03 = tileOffset.x * 0.5f;
                tileMatrix.m13 = tileOffset.y * 0.5f;
                worldToShadowCascadeMatrices[i] = tileMatrix * worldToShadowCascadeMatrices[i];
            }

            shadowBuffer.DisableScissorRect();
            shadowBuffer.SetGlobalTexture(cascadedShadowMapId, cascadedShadowMap);
            shadowBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            shadowBuffer.SetGlobalMatrixArray(worldToShadowCascadeMatricesId, worldToShadowCascadeMatrices);

            float invShadowMapSize = 1f / ShadowMapSize;
            shadowBuffer.SetGlobalVector(cascadedShadowMapSizeId, new Vector4(invShadowMapSize, invShadowMapSize, ShadowMapSize, ShadowMapSize));
            shadowBuffer.SetGlobalFloat(cascadedShadoStrengthId, shadowLight.shadowStrength);
            bool hard = shadowLight.shadows == LightShadows.Hard;
            CoreUtils.SetKeyword(shadowBuffer, cascadedShadowsHardKeyword, hard);
            CoreUtils.SetKeyword(shadowBuffer, cascadedShadowsSoftKeyword, !hard);

            shadowBuffer.EndSample(shadowBufferName);
            context.ExecuteCommandBuffer(shadowBuffer);
            shadowBuffer.Clear();
        }

        private void Setup(ScriptableRenderContext context, Camera camera)
        {
            // 设置渲染相关的相机参数，包含相机的各个矩阵和剪裁平面等。
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;
            cameraBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
            cameraBuffer.BeginSample(SampleName);
            CoreUtils.SetKeyword(cameraBuffer, SRPBatchingKeyword, GraphicsSettings.useScriptableRenderPipelineBatching);
            ExecuteBuffer(context, camera);
        }

        private void ExecuteBuffer(ScriptableRenderContext context, Camera camera)
        {
            context.ExecuteCommandBuffer(cameraBuffer);
            cameraBuffer.Clear();
        }

        private void Draws(ScriptableRenderContext context, Camera camera)
        {
            // 渲染，牵扯到渲染排序，所以先要进行一个相机的排序设置，这里Unity内置了一些默认的排序可以调用。
            SortingSettings sortSet = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            // 需要指定允许的 shader pass 类型，不然是不会渲染的。
            DrawingSettings drawSet = new DrawingSettings(unlitShaderTagId, sortSet);

            drawSet.enableDynamicBatching = UseDynamicBatching;
            drawSet.enableInstancing = UseInstancing;
            if (cullingResults.visibleLights.Length > 0)
            {
                // 预计算每个物体受哪些光源的影响，信息被存在 UnityPerDraw Buffer 的 unity_LightData 和 unity_LightIndices 字段中。
                drawSet.perObjectData = PerObjectData.LightData | PerObjectData.LightIndices;
            }
            drawSet.perObjectData |= PerObjectData.ReflectionProbes; // 反射环境。
            drawSet.perObjectData |= PerObjectData.Lightmaps; // 采样光照贴图。
            drawSet.perObjectData |= PerObjectData.LightProbe; // 采样光照探针。
            drawSet.perObjectData |= PerObjectData.LightProbeProxyVolume; // 采样多个光照探针。

            // 过滤，这边是指定渲染的队列（对应shader中的RenderQueue）和相关Layer的设置（-1表示全部layer）。
            FilteringSettings filtSet = new FilteringSettings(RenderQueueRange.opaque);

            context.DrawRenderers(cullingResults, ref drawSet, ref filtSet);

            context.DrawSkybox(camera);
            // 透明物体和Scene窗口的UI需要在渲染完天空盒之后再渲染。
            sortSet.criteria = SortingCriteria.CommonTransparent;
            drawSet.sortingSettings = sortSet;
            filtSet.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cullingResults, ref drawSet, ref filtSet);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")] // 如果这两个宏不启用，则该方法不执行。
        private void DrawUnsupportedShaders(ScriptableRenderContext context, Camera camera)
        {
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            SortingSettings sortSet = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            DrawingSettings drawSet = new DrawingSettings(legacyShaderTagIds[0], sortSet)
            {
                overrideMaterial = errorMaterial
            };
            for (int i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawSet.SetShaderPassName(i, legacyShaderTagIds[i]);
            }

            drawSet.enableDynamicBatching = UseDynamicBatching;
            drawSet.enableInstancing = UseInstancing;

            FilteringSettings filtSet = FilteringSettings.defaultValue;

            context.DrawRenderers(cullingResults, ref drawSet, ref filtSet);
        }

        private void DrawGizmos(ScriptableRenderContext context, Camera camera)
        {
            if (UnityEditor.Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }

        private void Submit(ScriptableRenderContext context, Camera camera)
        {
            cameraBuffer.EndSample(SampleName);
            ExecuteBuffer(context, camera);
            // 开始执行。
            context.Submit();
        }
    }
}
