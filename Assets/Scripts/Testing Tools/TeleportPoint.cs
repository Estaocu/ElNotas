using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

public class TeleportPoint : MonoBehaviour
{
    [SerializeField, Range(-1, 9)] private int number = -1;

    public int Number => number;

    private void OnValidate()
    {
        if (number < -1) number = -1;
        else if (number > 9) number = 9;
    }

    private void OnDrawGizmos()
    {
        if (number < 0) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
#if UNITY_EDITOR
        Handles.Label(transform.position + Vector3.up * 0.8f, number.ToString());
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TeleportPoint))]
public class TeleportPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (target == null) return;
        serializedObject.Update();

        TeleportPoint tp = (TeleportPoint)target;
        SerializedProperty numberProp = serializedObject.FindProperty("number");

        TeleportPoint[] allPoints = FindObjectsOfType<TeleportPoint>();
        var usedNumbers = new List<int>();
        foreach (TeleportPoint point in allPoints)
        {
            if (point != tp && point.Number >= 0)
                usedNumbers.Add(point.Number);
        }

        string[] labels = new string[11];
        int[] values = new int[11];
        labels[0] = "-- None --";
        values[0] = -1;
        for (int i = 0; i < 10; i++)
        {
            values[i + 1] = i;
            labels[i + 1] = usedNumbers.Contains(i) ? $"{i} (used)" : $"{i}";
        }

        int currentValue = numberProp.intValue;
        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup("Number", currentIndex, labels);
        if (newIndex != currentIndex)
        {
            numberProp.intValue = values[newIndex];
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
