using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine;

public class EasyPipeline : RenderPipeline
{
    public bool UseDynamicBatching;
    public bool UseSRPBatching;
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

    private string SampleName { get; set; }

    // 这个函数会在绘制管线时调用，两个参数，第一个为所有渲染相关的内容（不光只有
    // 渲染目标，同时还有灯光，反射探针，光照探针等等相关的东西），第二个为相机组。
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = UseSRPBatching;

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
        ExecuteBuffer(context, camera);
    }

    private void ExecuteBuffer(ScriptableRenderContext context, Camera camera)
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void DrawVisibleGeometry(ScriptableRenderContext context, Camera camera)
    {
        // 渲染，牵扯到渲染排序，所以先要进行一个相机的排序设置，这里Unity内置了一些默认的排序可以调用
        SortingSettings sortSet = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        // 需要指定允许的 shader pass 类型，不然是不会渲染的。
        DrawingSettings drawSet = new DrawingSettings(unlitShaderTagId, sortSet);

        drawSet.enableDynamicBatching = UseDynamicBatching;
        drawSet.enableInstancing = UseInstancing;

        //过滤，这边是指定渲染的队列（对应shader中的RenderQueue）和相关Layer的设置（-1表示全部layer）
        FilteringSettings filtSet = new FilteringSettings(RenderQueueRange.all);

        context.DrawRenderers(cullingResults, ref drawSet, ref filtSet);

        // 绘制天空盒。
        context.DrawSkybox(camera);
    }

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
