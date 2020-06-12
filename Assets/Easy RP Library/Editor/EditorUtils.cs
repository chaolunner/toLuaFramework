using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace UniEasy.Rendering
{
    public static class EditorUtils
    {
        private static Assembly URPEditorAssembly;
        private static Type ShadowCascadeSplitGUIType;
        private static MethodInfo HandleCascadeSliderGUIMethodInfo;

        public static void HandleCascadeSliderGUI(ref float[] cascadePartitionSizes)
        {
            if (cascadePartitionSizes == null) { return; }
            if (URPEditorAssembly == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (assemblies[i].GetName().Name == "Unity.RenderPipelines.Universal.Editor")
                    {
                        URPEditorAssembly = assemblies[i];
                        break;
                    }
                }
            }
            if (URPEditorAssembly == null) { return; }
            if (ShadowCascadeSplitGUIType == null)
            {
                Type[] types = URPEditorAssembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].Name == "ShadowCascadeSplitGUI")
                    {
                        ShadowCascadeSplitGUIType = types[i];
                        break;
                    }
                }
            }
            if (ShadowCascadeSplitGUIType == null) { return; }
            if (HandleCascadeSliderGUIMethodInfo == null)
            {
                HandleCascadeSliderGUIMethodInfo = ShadowCascadeSplitGUIType.GetMethod("HandleCascadeSliderGUI", BindingFlags.Static | BindingFlags.Public);
            }
            if (HandleCascadeSliderGUIMethodInfo == null) { return; }
            HandleCascadeSliderGUIMethodInfo.Invoke(null, new object[] { cascadePartitionSizes });
        }

        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;
            Type type = typeof(T);
            if (type == typeof(float))
            {
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            }
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }
            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }
    }
}
