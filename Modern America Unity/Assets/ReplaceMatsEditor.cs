#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if UNITY_EDITOR
[CustomEditor(typeof(ReplaceMats))]
public class ReplaceMatsEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ReplaceMats script = (ReplaceMats)target;

        if (GUILayout.Button("Replace All Materials"))
        {
            script.PerformReplacement();
        }
    }

}
#endif