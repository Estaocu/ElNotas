#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Melody))]
public class MelodyEditor : Editor
{
    private SerializedProperty melodyNameProp;
    private SerializedProperty notesProp;
    private SerializedProperty colorProp;

    private void OnEnable()
    {
        melodyNameProp = serializedObject.FindProperty("melodyName");
        notesProp = serializedObject.FindProperty("notes");
        colorProp = serializedObject.FindProperty("color");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Melody Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(melodyNameProp, new GUIContent("Melody Name"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Notes", EditorStyles.boldLabel);

        // Selector de tamaño del array
        int newSize = EditorGUILayout.IntSlider("Note Count", notesProp.arraySize, 1, 4);
        if (newSize != notesProp.arraySize)
            notesProp.arraySize = newSize;

        // Mostrar cada elemento con etiqueta "Note 1/2/3/4"
        for (int i = 0; i < notesProp.arraySize; i++)
        {
            var element = notesProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element, new GUIContent($"Note {i + 1}"));
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(colorProp, new GUIContent("Soundwave Color"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
