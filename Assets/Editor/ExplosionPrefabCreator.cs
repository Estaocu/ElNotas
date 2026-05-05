using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ExplosionPrefabCreator : Editor
{
    [MenuItem("Tools/El Notas/Create Explosion Prefab")]
    public static void CreateExplosionPrefab()
    {
        string vfxPath = "Assets/VFX/SH_RealisticExplosion.png";
        string sfxPath = "Assets/SFX/snd_badexplosion.wav";
        string resourcesPath = "Assets/Resources";
        string prefabName = "ExplosionDecalPrefab.prefab";
        string fullPrefabPath = Path.Combine(resourcesPath, prefabName);

        // 1. Load Assets
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);
        Sprite[] sprites = allAssets.OfType<Sprite>().OrderBy(s => s.name).ToArray();
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath);

        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites found at {vfxPath}. Make sure it's sliced!");
            return;
        }

        if (clip == null)
        {
            Debug.LogError($"AudioClip not found at {sfxPath}!");
            return;
        }

        // 2. Create GameObject
        GameObject go = new GameObject("ExplosionDecal");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        AudioSource audio = go.AddComponent<AudioSource>();
        ExplosionDecal logic = go.AddComponent<ExplosionDecal>();

        // 3. Configure Components
        sr.sprite = sprites[0];
        audio.clip = clip;
        audio.playOnAwake = false; // Script handles playing
        
        // Use reflection or serialized object to set private fields if needed, 
        // but since they are [SerializeField], we can use SerializedObject.
        SerializedObject so = new SerializedObject(logic);
        SerializedProperty framesProp = so.FindProperty("animationFrames");
        
        framesProp.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
        so.ApplyModifiedProperties();

        // 4. Save as Prefab
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, fullPrefabPath);
        
        // 5. Cleanup
        Object.DestroyImmediate(go);

        if (prefab != null)
        {
            Debug.Log($"Successfully created explosion prefab at {fullPrefabPath}");
            Selection.activeObject = prefab;
        }
        else
        {
            Debug.LogError("Failed to create prefab!");
        }
    }
}
