using UnityEngine;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

public enum EntryType { Word, Melody }
public enum WordCategory { Places, Music, Creatures, Other }
public enum PageSize { Half, One, Two }

[CreateAssetMenu(fileName = "NewNotebookEntry", menuName = "ScriptableObjects/Notebook Entry", order = 1)]
public class NotebookEntry : ScriptableObject
{
    public string key;
    public LocalizedString id;
    public EntryType type;

    public bool hasCategory;
    public WordCategory category;

    public GameObject visual;
    public PageSize size;
    public notesEnum[] melody;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        bake();
    }

    public void bake()
    {
        if (id == null || id.IsEmpty) return;

        StringTableCollection collection =
            LocalizationEditorSettings.GetStringTableCollection(id.TableReference);
        if (collection == null) return;

        SharedTableData.SharedTableEntry sharedEntry =
            collection.SharedData.GetEntryFromReference(id.TableEntryReference);
        if (sharedEntry == null) return;

        // Only touch the data once we know the source exists
        key = sharedEntry.Key;

        if (key.StartsWith("ne_w_")) type = EntryType.Word;
        else if (key.StartsWith("ne_m_")) type = EntryType.Melody;

        MelodyMetadata melodyMetadata = sharedEntry.Metadata.GetMetadata<MelodyMetadata>();
        melody = melodyMetadata != null && melodyMetadata.notes != null
            ? (notesEnum[])melodyMetadata.notes.Clone()
            : null;

        WordCategoryMetadata categoryMetadata = sharedEntry.Metadata.GetMetadata<WordCategoryMetadata>();
        hasCategory = categoryMetadata != null;
        category = hasCategory ? categoryMetadata.category : default;

        EditorUtility.SetDirty(this);
    }
#endif
}