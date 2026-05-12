using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reports stale persistent listeners on TriggerDetector components.
/// The previous API used UnityEvent&lt;Collider&gt; / UnityEvent&lt;GameObject&gt;; the new API
/// uses parameterless UnityEvents (OnDetected / OnLost). Old wirings under the legacy
/// field names (onTriggerEntered / onTriggerExited) survive in the YAML but are orphaned.
/// This tool surfaces them so the team can re-wire each prefab deliberately.
/// </summary>
public static class TriggerDetectorMigration
{
    private const string ReportPath = "TriggerDetectorMigrationReport.txt";

    [MenuItem("Tools/El Notas/Scan Stale TriggerDetector Listeners")]
    public static void Scan()
    {
        var sb = new StringBuilder();
        sb.AppendLine("TriggerDetector migration report");
        sb.AppendLine("================================");
        sb.AppendLine();

        var hits = 0;
        hits += ScanPrefabs(sb);
        hits += ScanScenes(sb);

        sb.AppendLine();
        sb.AppendLine($"Total stale listeners found: {hits}");

        File.WriteAllText(ReportPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[TriggerDetectorMigration] Scan complete. {hits} stale listeners. Report: {Path.GetFullPath(ReportPath)}");
    }

    [MenuItem("Tools/El Notas/Clear Stale TriggerDetector Listeners")]
    public static void Clear()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear stale TriggerDetector listeners?",
                "This will remove orphaned persistent listeners on TriggerDetector components in prefabs and open scenes. The data cannot be recovered — run 'Scan' first and save the report so you know what to re-wire.\n\nContinue?",
                "Clear", "Cancel"))
            return;

        var cleared = 0;
        cleared += ClearPrefabs();
        cleared += ClearOpenScenes();

        AssetDatabase.SaveAssets();
        Debug.Log($"[TriggerDetectorMigration] Cleared {cleared} stale listeners.");
    }

    private static int ScanPrefabs(StringBuilder sb)
    {
        var hits = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            foreach (var td in prefab.GetComponentsInChildren<TriggerDetector>(true))
            {
                hits += DescribeOrphans(td, $"Prefab: {path} / {td.transform.name}", sb);
            }
        }
        return hits;
    }

    private static int ScanScenes(StringBuilder sb)
    {
        var hits = 0;
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var td in root.GetComponentsInChildren<TriggerDetector>(true))
                {
                    hits += DescribeOrphans(td, $"Scene: {scene.name} / {GetPath(td.transform)}", sb);
                }
            }
        }
        return hits;
    }

    private static int DescribeOrphans(TriggerDetector td, string label, StringBuilder sb)
    {
        var so = new SerializedObject(td);
        var hits = 0;
        foreach (var legacyField in new[] { "onTriggerEntered", "onTriggerExited" })
        {
            var prop = so.FindProperty(legacyField);
            if (prop == null) continue;
            var calls = prop.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null || calls.arraySize == 0) continue;

            sb.AppendLine($"- {label}");
            sb.AppendLine($"    legacy field: {legacyField}  ({calls.arraySize} listener(s))");
            for (var i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var target = call.FindPropertyRelative("m_Target")?.objectReferenceValue;
                var method = call.FindPropertyRelative("m_MethodName")?.stringValue;
                var argType = call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName")?.stringValue;
                sb.AppendLine($"      [{i}] target={target}  method={method}  argType={argType}");
                hits++;
            }
        }
        return hits;
    }

    private static int ClearPrefabs()
    {
        var cleared = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            var dirty = false;
            foreach (var td in root.GetComponentsInChildren<TriggerDetector>(true))
                dirty |= ClearOnComponent(td);

            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                cleared++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        return cleared;
    }

    private static int ClearOpenScenes()
    {
        var cleared = 0;
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var anyDirty = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var td in root.GetComponentsInChildren<TriggerDetector>(true))
                {
                    if (ClearOnComponent(td))
                    {
                        anyDirty = true;
                        cleared++;
                    }
                }
            }
            if (anyDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        return cleared;
    }

    private static bool ClearOnComponent(TriggerDetector td)
    {
        var so = new SerializedObject(td);
        var changed = false;
        foreach (var legacyField in new[] { "onTriggerEntered", "onTriggerExited" })
        {
            var prop = so.FindProperty(legacyField);
            if (prop == null) continue;
            var calls = prop.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null || calls.arraySize == 0) continue;
            calls.ClearArray();
            changed = true;
        }
        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }

    private static string GetPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null) { t = t.parent; path = $"{t.name}/{path}"; }
        return path;
    }
}
