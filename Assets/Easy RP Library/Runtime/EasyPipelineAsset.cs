using UnityEngine.Rendering;
using UnityEngine;

public enum ShadowMapSize
{
    _256 = 256,
    _512 = 512,
    _1024 = 1024,
    _2048 = 2048,
    _4096 = 4096
}

[CreateAssetMenu(menuName = "Rendering/Easy Render Pipeline/Pipeline Asset")]
public class EasyPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool useDynamicBatching;
    [SerializeField] private bool useSRPBatching;
    [SerializeField] private bool useInstancing;
    [SerializeField] private ShadowMapSize shadowMapSize = ShadowMapSize._1024;

    protected override RenderPipeline CreatePipeline()
    {
        // 创建自定义渲染管线并返回。
        return new EasyPipeline(useSRPBatching) { UseDynamicBatching = useDynamicBatching, UseInstancing = useInstancing, ShadowMapSize = (int)shadowMapSize };
    }
}
