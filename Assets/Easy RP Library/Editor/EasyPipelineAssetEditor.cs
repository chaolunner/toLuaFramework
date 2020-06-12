using UnityEditor;
using UnityEngine;

namespace UniEasy.Rendering
{
    [CustomEditor(typeof(EasyPipelineAsset))]
    public class EasyPipelineAssetEditor : Editor
    {
        SerializedProperty shadowCascades;
        SerializedProperty twoCascadesSplit;
        SerializedProperty fourCascadesSplit;

        void OnEnable()
        {
            shadowCascades = serializedObject.FindProperty("shadowCascades");
            twoCascadesSplit = serializedObject.FindProperty("twoCascadesSplit");
            fourCascadesSplit = serializedObject.FindProperty("fourCascadesSplit");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            switch (shadowCascades.enumValueIndex)
            {
                case 0: return;
                case 1:
                    EditorUtils.DrawCascadeSplitGUI<float>(ref twoCascadesSplit);
                    break;
                case 2:
                    EditorUtils.DrawCascadeSplitGUI<Vector3>(ref fourCascadesSplit);
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
