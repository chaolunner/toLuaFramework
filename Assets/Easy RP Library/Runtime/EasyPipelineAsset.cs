using UnityEngine.Rendering;
using UnityEngine;

namespace UniEasy.Rendering
{
    public enum ShadowMapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum ShadowCascades
    {
        Zero = 0,
        Two = 2,
        Four = 4
    }

    [CreateAssetMenu(menuName = "Rendering/Easy Render Pipeline/Pipeline Asset")]
    public class EasyPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] private bool useDynamicBatching;
        [SerializeField] private bool useSRPBatching;
        [SerializeField] private bool useInstancing;
        [SerializeField] private ShadowMapSize shadowMapSize = ShadowMapSize._1024;
        [SerializeField] private float shadowDistance = 100f;
        [SerializeField] private ShadowCascades shadowCascades = ShadowCascades.Four;
        [SerializeField, Range(0.01f, 2f)] private float shadowFadeRange = 1f;

        [SerializeField, HideInInspector]
        private float twoCascadesSplit = 0.25f;
        [SerializeField, HideInInspector]
        private Vector3 fourCascadesSplit = new Vector3(0.067f, 0.2f, 0.467f);

        protected override RenderPipeline CreatePipeline()
        {
            Vector3 shadowCascadeSplit = shadowCascades == ShadowCascades.Four ? fourCascadesSplit : new Vector3(twoCascadesSplit, 0f);
            // 创建自定义渲染管线并返回。
            return new EasyPipeline(useSRPBatching, shadowFadeRange)
            {
                UseDynamicBatching = useDynamicBatching,
                UseInstancing = useInstancing,
                ShadowMapSize = (int)shadowMapSize,
                ShadowDistance = shadowDistance,
                ShadowCascades = (int)shadowCascades,
                ShadowCascadeSplit = shadowCascadeSplit,
            };
        }
    }
}
