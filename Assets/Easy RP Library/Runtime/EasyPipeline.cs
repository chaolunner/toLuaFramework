using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute; // 与 UnityEngine.Debug 类冲突，所以取个别名。

public class EasyPipeline : RenderPipeline
{
    public bool UseDynamicBatching;
    public bool UseInstancing;
    public int ShadowMapSize;

    private CommandBuffer cameraBuffer = new CommandBuffer()
    {
        name = cameraBufferName
    };
    private CullingResults cullingResults;
    private const string cameraBufferName = "Render Camera";
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
    // 光源
    private Vector4[] visibleLightColors = new Vector4[maxVisibleLights];
    private Vector4[] visibleLightDirectionsOrPositions = new Vector4[maxVisibleLights];
    private Vector4[] visibleLightAttenuations = new Vector4[maxVisibleLights];
    private Vector4[] visibleLightSpotDirections = new Vector4[maxVisibleLights];
    private const int maxVisibleLights = 16;
    private static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
    private static int visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    private static int visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
    private static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");
    private static int unity_LightDataID = Shader.PropertyToID("unity_LightData");
    // 阴影
    private RenderTexture shadowMap;
    private CommandBuffer shadowBuffer = new CommandBuffer
    {
        name = shadowBufferName
    };
    private Vector4[] shadowData = new Vector4[maxVisibleLights];
    private Matrix4x4[] worldToShadowMatrices = new Matrix4x4[maxVisibleLights];
    private int shadowTileCount;
    private const string shadowBufferName = "Render Shadows";
    private const string shadowsHardKeyword = "_SHADOWS_HARD";
    private const string shadowsSoftKeyword = "_SHADOWS_SOFT";
    private static int worldToShadowMatricesId = Shader.PropertyToID("_WorldToShadowMatrices");
    private static int shadowMapId = Shader.PropertyToID("_ShadowMap");
    private static int shadowBiasId = Shader.PropertyToID("_ShadowBias");
    private static int shadowDataId = Shader.PropertyToID("_ShadowData");
    private static int shadowMapSizeId = Shader.PropertyToID("_ShadowMapSize");

    private string SampleName { get; set; }

    public EasyPipeline(bool useSRPBatching)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
        GraphicsSettings.lightsUseLinearIntensity = true; // 采用线性空间，而不是gamma空间。
    }

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
            }
            //ConfigureLights();
            Setup(context, cameras[i]);
            DrawVisibleGeometry(context, cameras[i]);
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
        // 直接使用相机默认剪裁参数
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
        {
            // 获取剪裁之后的全部结果（其中不仅有渲染物体，还有相关的其他渲染要素）
            cullingResults = context.Cull(ref cullParam);
            return true;
        }
        return false;
    }

    private void ConfigureLights()
    {
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
                    Light shadowLight = light.light;
                    Bounds shadowBounds;
                    if (shadowLight.shadows != LightShadows.None && cullingResults.GetShadowCasterBounds(i, out shadowBounds))
                    {
                        shadowTileCount += 1;
                        shadow.x = shadowLight.shadowStrength;
                        shadow.y = shadowLight.shadows == LightShadows.Soft ? 1f : 0f;
                    }
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
        if (cullingResults.visibleLights.Length > maxVisibleLights) { return; }
        var lightIndexs = cullingResults.GetLightIndexMap(Unity.Collections.Allocator.TempJob);
        for (int i = maxVisibleLights; i < cullingResults.visibleLights.Length; i++)
        {
            lightIndexs[i] = -1;
        }
        cullingResults.SetLightIndexMap(lightIndexs);
        lightIndexs.Dispose();
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

        shadowMap = RenderTexture.GetTemporary(ShadowMapSize, ShadowMapSize, 16, RenderTextureFormat.Shadowmap);
        shadowMap.filterMode = FilterMode.Bilinear;
        shadowMap.wrapMode = TextureWrapMode.Clamp;

        // 告诉GPU将渲染信息写入阴影贴图中。
        // 我们会清理这个纹理，所以我们并不关注它来自哪，可以用 RenderBufferLoadAction.DontCare 来指明这一点，这将使得tile-based架构的GPU有更高的执行效率。
        // 我们随后需要采样该纹理，所以需要将其存储在内存中，通过 RenderBufferStoreAction.Store 来指明这一点。
        // 我们只关注深度通道，通过 ClearFlag.Depth 来指明这一点。
        CoreUtils.SetRenderTarget(shadowBuffer, shadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Depth);

        shadowBuffer.BeginSample(shadowBufferName);
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
        int tileIndex = 0;
        bool hardShadows = false;
        bool softShadows = false;
        for (int i = 0; i < cullingResults.visibleLights.Length; i++)
        {
            if (i == maxVisibleLights)
            {
                break;
            }
            if (shadowData[i].x <= 0f)
            {
                continue;
            }
            // 我们从光源的视角渲染场景，就犹如我们将聚光灯看做是一个摄像机一样。
            // 因此，我们需要提供适当的视角投影矩阵。我们可以通过 CullingResults 中的 ComputeSpotShadowMatricesAndCullingPrimitives 方法得到该矩阵。
            // 该方法的第一个参数是光源序列，视野矩阵 和 投影矩阵 则是在后两个输出参数中。
            // 最后一个参数 ShadowSplitData 我们用不到，但作为输出参数，我们必须提供。
            Matrix4x4 viewMatrix, projectionMatrix;
            ShadowSplitData splitData;
            if (!cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(i, out viewMatrix, out projectionMatrix, out splitData))
            {
                shadowData[i].x = 0f;
                continue;
            }
            if (shadowData[i].y <= 0f) { hardShadows = true; }
            else { softShadows = true; }
            // 设置适当的阴影图集区域。
            float tileOffsetX = tileIndex % split;
            float tileOffsetY = tileIndex / split;
            tileIndex += 1;
            tileViewport.x = tileOffsetX * tileSize;
            tileViewport.y = tileOffsetY * tileSize;
            if (split > 1)
            {
                shadowBuffer.SetViewport(tileViewport);
                // 给阴影图集之间保留一点间距，防止采样时出错。
                shadowBuffer.EnableScissorRect(new Rect(
                    tileViewport.x + 4f, tileViewport.y + 4f,
                    tileSize - 8f, tileSize - 8f
                ));
            }
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
            worldToShadowMatrices[i] = scaleOffset * (projectionMatrix * viewMatrix);
            // 最后要做的就是调整world-to-shadow矩阵，让它能采样到正确的平铺块。
            // 我们可以乘以一个有适当xy偏移的转换矩阵。shader不需要关心我们是否使用了图集。
            if (split > 1)
            {
                var tileMatrix = Matrix4x4.identity;
                tileMatrix.m00 = tileMatrix.m11 = tileScale;
                tileMatrix.m03 = tileOffsetX * tileScale;
                tileMatrix.m13 = tileOffsetY * tileScale;
                worldToShadowMatrices[i] = tileMatrix * worldToShadowMatrices[i];
            }
            shadowBuffer.SetGlobalMatrixArray(worldToShadowMatricesId, worldToShadowMatrices);
        }
        if (split > 1)
        {
            shadowBuffer.DisableScissorRect(); // 我们在渲染阴影后调用 DisableScissorRect 关闭裁剪矩形，不然会影响到后面的常规渲染。
        }
        shadowBuffer.SetGlobalTexture(shadowMapId, shadowMap);
        shadowBuffer.SetGlobalVectorArray(shadowDataId, shadowData);

        CoreUtils.SetKeyword(shadowBuffer, shadowsHardKeyword, hardShadows);
        CoreUtils.SetKeyword(shadowBuffer, shadowsSoftKeyword, softShadows);

        float invShadowMapSize = 1f / ShadowMapSize;
        shadowBuffer.SetGlobalVector(shadowMapSizeId, new Vector4(invShadowMapSize, invShadowMapSize, ShadowMapSize, ShadowMapSize));

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
        ExecuteBuffer(context, camera);
    }

    private void ExecuteBuffer(ScriptableRenderContext context, Camera camera)
    {
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
    }

    private void DrawVisibleGeometry(ScriptableRenderContext context, Camera camera)
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

        // 过滤，这边是指定渲染的队列（对应shader中的RenderQueue）和相关Layer的设置（-1表示全部layer）
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
