using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute; // 与 UnityEngine.Debug 类冲突，所以取个别名。

public class EasyPipeline : RenderPipeline
{
    public bool UseDynamicBatching;
    public bool UseInstancing;

    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };
    private CullingResults cullingResults;
    private const string bufferName = "Render Camera";
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

            Setup(context, cameras[i]);
            DrawVisibleGeometry(context, cameras[i]);
            DrawUnsupportedShaders(context, cameras[i]);
#if UNITY_EDITOR
            DrawGizmos(context, cameras[i]);
#endif
            Submit(context, cameras[i]);
        }
    }

    private void PrepareBuffer(Camera camera)
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
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

    private void Setup(ScriptableRenderContext context, Camera camera)
    {
        // 设置渲染相关的相机参数，包含相机的各个矩阵和剪裁平面等。
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);

        if (cullingResults.visibleLights.Length > 0)
        {
            ConfigureLights();
        }
        else
        {
            // 当没有光源时，清除关于光源数量的缓存。
            buffer.SetGlobalVector(unity_LightDataID, Vector4.zero);
        }

        ExecuteBuffer(context, camera);
    }

    private void ConfigureLights()
    {
        for (int i = 0; i < cullingResults.visibleLights.Length; i++)
        {
            if (i == maxVisibleLights) { break; }
            VisibleLight light = cullingResults.visibleLights[i];
            // finalColor字段存储了光源的颜色，该颜色数据是由光源的color属性和intensity属性相乘后的结果，并经过了颜色空间的校正。
            visibleLightColors[i] = light.finalColor;
            Vector4 attenuation = Vector4.zero;
            attenuation.w = 1f; // 为了保证不同类型的光照计算的一致性（用同样的shader代码），将w分量设置为1。
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
                }
            }
            // 填充点光源或聚光灯的光照范围。
            visibleLightAttenuations[i] = attenuation;
        }

        buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
        buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
        buffer.SetGlobalVectorArray(visibleLightAttenuationsId, visibleLightAttenuations); // 点光源。
        buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections); // 聚光灯。

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

    private void ExecuteBuffer(ScriptableRenderContext context, Camera camera)
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
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
        buffer.EndSample(SampleName);
        ExecuteBuffer(context, camera);
        // 开始执行。
        context.Submit();
    }
}
