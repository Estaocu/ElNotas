using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Singer))]
public class SingerEditor : Editor
{
    private SerializedProperty databaseProp;
    private SerializedProperty soundwavePrefabProp;
    private SerializedProperty soundwaveSpawnpointProp;
    private SerializedProperty instrumentProp;
    private SerializedProperty voiceProp;
    private SerializedProperty cooldownProp;
    private SerializedProperty patternModeProp;
    private SerializedProperty patternProp;
    private SerializedProperty humanizationProp;

    private void OnEnable()
    {
        if (target == null) return;
        databaseProp = serializedObject.FindProperty("database");
        soundwavePrefabProp = serializedObject.FindProperty("soundwavePrefab");
        soundwaveSpawnpointProp = serializedObject.FindProperty("soundwaveSpawnpoint");
        instrumentProp = serializedObject.FindProperty("instrument");
        voiceProp = serializedObject.FindProperty("voice");
        cooldownProp = serializedObject.FindProperty("cooldown");
        patternModeProp = serializedObject.FindProperty("patternMode");
        patternProp = serializedObject.FindProperty("pattern");
        humanizationProp = serializedObject.FindProperty("humanizationPercent");
    }

    public override void OnInspectorGUI()
    {
        if (target == null || databaseProp == null) return;
        serializedObject.Update();

        bool isPlayer = instrumentProp.objectReferenceValue != null;

        if (isPlayer)
        {
            // Singer del jugador: inspector por defecto, sin grid.
            DrawDefaultInspector();
            return;
        }

        // Singer NPC: layout personalizado con grid 4x4.
        EditorGUILayout.LabelField("Core", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(databaseProp);
        EditorGUILayout.PropertyField(soundwavePrefabProp);
        EditorGUILayout.PropertyField(soundwaveSpawnpointProp);
        EditorGUILayout.PropertyField(instrumentProp);
        EditorGUILayout.PropertyField(voiceProp);
        EditorGUILayout.PropertyField(cooldownProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Rhythm Pattern (NPC)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternModeProp);
        EditorGUILayout.PropertyField(humanizationProp, new GUIContent("Humanization %"));

        EditorGUILayout.Space(6);
        EnsurePatternSize();
        DrawPatternGrid();

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsurePatternSize()
    {
        if (patternProp.arraySize != 16)
            patternProp.arraySize = 16;
    }

    private void DrawPatternGrid()
    {
        EditorGUILayout.LabelField("Subbeat grid (4 beats × 4 subbeats)");

        const float cellSize = 48f;
        var cellOpts = new GUILayoutOption[] { GUILayout.Width(cellSize), GUILayout.Height(cellSize) };
        Color originalBg = GUI.backgroundColor;

        for (int row = 0; row < 4; row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < 4; col++)
            {
                int index = row * 4 + col;
                var element = patternProp.GetArrayElementAtIndex(index);
                NoteSlot slot = ReadSlot(element);

                // Resaltado sutil en la primera columna (whole beat).
                GUI.backgroundColor = col == 0
                    ? new Color(1.0f, 0.85f, 0.5f, 1f)
                    : originalBg;

                string label = slot == NoteSlot.Empty ? "—" : ((int)slot + 1).ToString();

                if (GUILayout.Button(label, cellOpts))
                {
                    ShowSlotMenu(element);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = originalBg;
    }

    // enumValueIndex es el índice en la declaración (0 = Empty, 1 = Note1, ...),
    // no el valor subyacente. Mapear por nombre es robusto frente a futuros cambios.
    private NoteSlot ReadSlot(SerializedProperty element)
    {
        string name = element.enumNames[element.enumValueIndex];
        switch (name)
        {
            case "Note1": return NoteSlot.Note1;
            case "Note2": return NoteSlot.Note2;
            case "Note3": return NoteSlot.Note3;
            case "Note4": return NoteSlot.Note4;
            default: return NoteSlot.Empty;
        }
    }

    private void ShowSlotMenu(SerializedProperty element)
    {
        var menu = new GenericMenu();
        AddMenuItem(menu, element, "Empty", "Empty");
        AddMenuItem(menu, element, "1", "Note1");
        AddMenuItem(menu, element, "2", "Note2");
        AddMenuItem(menu, element, "3", "Note3");
        AddMenuItem(menu, element, "4", "Note4");
        menu.ShowAsContext();
    }

    private void AddMenuItem(GenericMenu menu, SerializedProperty element, string label, string enumName)
    {
        string captured = enumName;
        menu.AddItem(new GUIContent(label), false, () =>
        {
            int idx = System.Array.IndexOf(element.enumNames, captured);
            if (idx >= 0)
            {
                element.enumValueIndex = idx;
                serializedObject.ApplyModifiedProperties();
            }
        });
    }
}
