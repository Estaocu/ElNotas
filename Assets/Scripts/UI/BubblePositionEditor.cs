#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BubblePosition))]
public class BubblePositionEditor : Editor
{
    private SerializedProperty positionA;
    private SerializedProperty positionB;

    private void OnEnable()
    {
        positionA = serializedObject.FindProperty("positionA");
        positionB = serializedObject.FindProperty("positionB");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Position States", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(positionA, new GUIContent("Position A"));
        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(positionB, new GUIContent("Position B"));

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Save Current As Position A"))
        {
            SaveCurrentPosition(positionA);
        }

        if (GUILayout.Button("Save Current As Position B"))
        {
            SaveCurrentPosition(positionB);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Apply Position A"))
        {
            ApplyPosition(positionA);
        }

        if (GUILayout.Button("Apply Position B"))
        {
            ApplyPosition(positionB);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void SaveCurrentPosition(SerializedProperty positionProperty)
    {
        BubblePosition bubblePosition = (BubblePosition)target;
        RectTransform rectTransform = bubblePosition.GetComponent<RectTransform>();

        positionProperty.FindPropertyRelative("anchorMin").vector2Value =
            rectTransform.anchorMin;

        positionProperty.FindPropertyRelative("anchorMax").vector2Value =
            rectTransform.anchorMax;

        positionProperty.FindPropertyRelative("pivot").vector2Value =
            rectTransform.pivot;

        positionProperty.FindPropertyRelative("anchoredPosition").vector2Value =
            rectTransform.anchoredPosition;

        positionProperty.FindPropertyRelative("sizeDelta").vector2Value =
            rectTransform.sizeDelta;

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
    }

    private void ApplyPosition(SerializedProperty positionProperty)
    {
        BubblePosition bubblePosition = (BubblePosition)target;
        RectTransform rectTransform = bubblePosition.GetComponent<RectTransform>();

        Undo.RecordObject(rectTransform, "Apply Bubble Position");

        rectTransform.anchorMin =
            positionProperty.FindPropertyRelative("anchorMin").vector2Value;

        rectTransform.anchorMax =
            positionProperty.FindPropertyRelative("anchorMax").vector2Value;

        rectTransform.pivot =
            positionProperty.FindPropertyRelative("pivot").vector2Value;

        rectTransform.anchoredPosition =
            positionProperty.FindPropertyRelative("anchoredPosition").vector2Value;

        rectTransform.sizeDelta =
            positionProperty.FindPropertyRelative("sizeDelta").vector2Value;

        EditorUtility.SetDirty(rectTransform);
    }
}

#endif
