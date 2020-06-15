using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniEasy.Rendering
{
    public class LitShaderGUI : ShaderGUI
    {
        private enum ClipMode
        {
            Off, On, Shadows
        }

        private MaterialEditor editor;
        private Object[] materials;
        private MaterialProperty[] properties;
        private bool showPresets;

        CullMode Cull
        {
            set
            {
                FindProperty("_Cull", properties).floatValue = (float)value;
            }
        }

        BlendMode SrcBlend
        {
            set
            {
                FindProperty("_SrcBlend", properties).floatValue = (float)value;
            }
        }

        BlendMode DstBlend
        {
            set
            {
                FindProperty("_DstBlend", properties).floatValue = (float)value;
            }
        }

        bool ZWrite
        {
            set
            {
                FindProperty("_ZWrite", properties).floatValue = value ? 1 : 0;
            }
        }

        ClipMode Clipping
        {
            set
            {
                FindProperty("_Clipping", properties).floatValue = (float)value;
                SetKeywordEnabled("_CLIPPING_OFF", value == ClipMode.Off);
                SetKeywordEnabled("_CLIPPING_ON", value == ClipMode.On);
                SetKeywordEnabled("_CLIPPING_SHADOWS", value == ClipMode.Shadows);
            }
        }

        bool ReceiveShadows
        {
            set
            {
                FindProperty("_ReceiveShadows", properties).floatValue =
                    value ? 1 : 0;
                SetKeywordEnabled("_RECEIVE_SHADOWS", value);
            }
        }

        RenderQueue RenderQueue
        {
            set
            {
                foreach (Material m in materials)
                {
                    m.renderQueue = (int)value;
                }
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            editor = materialEditor;
            materials = materialEditor.targets;
            this.properties = properties;

            CastShadowsToggle();

            showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
            if (showPresets)
            {
                OpaquePreset();
                ClipPreset();
                ClipDoubleSidedPreset();
                FadePreset();
                FadeWithShadowsPreset();
            }
        }

        private void SetPassEnabled(string pass, bool enabled)
        {
            foreach (Material m in materials)
            {
                m.SetShaderPassEnabled(pass, enabled);
            }
        }

        private bool? IsPassEnabled(string pass) // 返回可以为空的布尔值。
        {
            bool enabled = ((Material)materials[0]).GetShaderPassEnabled(pass);
            for (int i = 1; i < materials.Length; i++)
            {
                if (enabled != ((Material)materials[i]).GetShaderPassEnabled(pass))
                {
                    return null;
                }
            }
            return enabled;
        }

        private void CastShadowsToggle()
        {
            bool? enabled = IsPassEnabled("ShadowCaster");
            if (!enabled.HasValue)
            {
                EditorGUI.showMixedValue = true;
                enabled = false;
            }
            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.Toggle("Cast Shadows", enabled.Value);
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Cast Shadows");
                SetPassEnabled("ShadowCaster", enabled.Value);
            }
            EditorGUI.showMixedValue = false;
        }

        private void SetKeywordEnabled(string keyword, bool enabled)
        {
            if (enabled)
            {
                foreach (Material m in materials)
                {
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in materials)
                {
                    m.DisableKeyword(keyword);
                }
            }
        }

        private void OpaquePreset()
        {
            if (!GUILayout.Button("Opaque")) { return; }
            editor.RegisterPropertyChangeUndo("Opague Preset");
            Clipping = ClipMode.Off;
            Cull = CullMode.Back;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            ReceiveShadows = true;
            SetPassEnabled("ShadowCaster", true);
            RenderQueue = RenderQueue.Geometry;
        }

        private void ClipPreset()
        {
            if (!GUILayout.Button("Clip")) { return; }
            editor.RegisterPropertyChangeUndo("Clip Preset");
            Clipping = ClipMode.On;
            Cull = CullMode.Back;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            ReceiveShadows = true;
            SetPassEnabled("ShadowCaster", true);
            RenderQueue = RenderQueue.AlphaTest;
        }

        private void ClipDoubleSidedPreset()
        {
            if (!GUILayout.Button("Clip Double-Sided")) { return; }
            editor.RegisterPropertyChangeUndo("Clip Double-Sided Preset");
            Clipping = ClipMode.On;
            Cull = CullMode.Off;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            ReceiveShadows = true;
            SetPassEnabled("ShadowCaster", true);
            RenderQueue = RenderQueue.AlphaTest;
        }

        private void FadePreset()
        {
            if (!GUILayout.Button("Fade")) { return; }
            editor.RegisterPropertyChangeUndo("Fade Preset");
            Clipping = ClipMode.Off;
            Cull = CullMode.Back;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            ReceiveShadows = false;
            SetPassEnabled("ShadowCaster", false);
            RenderQueue = RenderQueue.Transparent;
        }

        private void FadeWithShadowsPreset()
        {
            if (!GUILayout.Button("Fade with Shadows")) { return; }
            editor.RegisterPropertyChangeUndo("Fade with Shadows Preset");
            Clipping = ClipMode.Shadows;
            Cull = CullMode.Back;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            ReceiveShadows = true;
            SetPassEnabled("ShadowCaster", true);
            RenderQueue = RenderQueue.Transparent;
        }
    }
}
