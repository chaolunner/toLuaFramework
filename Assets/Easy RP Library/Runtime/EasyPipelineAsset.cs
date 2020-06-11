using UnityEngine.Rendering;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Easy Render Pipeline/Pipeline Asset")]
public class EasyPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool useDynamicBatching;
    [SerializeField] private bool useSRPBatching;
    [SerializeField] private bool useInstancing;

    protected override RenderPipeline CreatePipeline()
    {
        // 创建自定义渲染管线并返回。
        return new EasyPipeline(useSRPBatching) { UseDynamicBatching = useDynamicBatching, UseInstancing = useInstancing };
    }
}
