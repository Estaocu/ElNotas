using UnityEngine;

public static class NoteSystem
{
    private static GameObject singleNotePrefab;
    private static GameObject melodyPrefab;

    private static void EnsurePrefabsLoaded()
    {
        if (singleNotePrefab == null)
            singleNotePrefab = Resources.Load<GameObject>("SingleNoteSoundwave");
        
        if (melodyPrefab == null)
            melodyPrefab = Resources.Load<GameObject>("Soundwave");

        if (singleNotePrefab == null || melodyPrefab == null)
            Debug.LogError("[NoteSystem] No se encontraron los prefabs en Resources. Revisa 'SingleNoteSoundwave' y 'Soundwave'.");
    }

    public static void EmitSingleNote(notesEnum note, Vector3 position, GameObject author)
    {
        EnsurePrefabsLoaded();
        if (singleNotePrefab == null) return;

        GameObject instance = Object.Instantiate(singleNotePrefab, position, Quaternion.identity);
        var script = instance.GetComponent<SingleNoteSoundwave>();
        if (script != null)
        {
            script.author = author;
            script.Expand(note);
        }
    }

    public static void EmitMelody(Melody melody, Vector3 position, Singer source)
    {
        EnsurePrefabsLoaded();
        if (melodyPrefab == null) return;

        GameObject instance = Object.Instantiate(melodyPrefab, position, Quaternion.identity);
        var soundwave = instance.GetComponent<Soundwave>();
        if (soundwave != null)
        {
            soundwave.myMelody = melody;
            soundwave.source = source;
        }
    }
}
