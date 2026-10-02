using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class NotebookEntryBaker : BuildPlayerProcessor
{
    public override int callbackOrder => -1000; // before Addressables

    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        bakeAll();
    }

    [MenuItem("Tools/Bake Notebook Entries")]
    public static void bakeAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:NotebookEntry");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NotebookEntry entry = AssetDatabase.LoadAssetAtPath<NotebookEntry>(path);
            if (entry != null) entry.bake();
        }
        AssetDatabase.SaveAssets();
    }
}