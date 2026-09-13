using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarterGames.Assets.SaveManager;
using Save;
using UnityEngine.Localization.SmartFormat.Utilities;

public class NotebookManager : MonoBehaviour
{
    public static NotebookManager Instance { get; private set; }

    [Header("Database (Inspector)")]
    [SerializeField]
    private List<NotebookEntry> totalEntryList =
        new List<NotebookEntry>();

    private readonly List<NotebookEntry> learnedEntriesList =
        new List<NotebookEntry>();

    private readonly List<CategoryGroup> learnedCategoriesRuntime =
        new List<CategoryGroup>();

    private NotebookSaveObject saveObject;
    private bool isLoading;
    private bool hasLoaded;

    public IReadOnlyList<NotebookEntry> TotalEntries =>
        totalEntryList;

    public IReadOnlyList<NotebookEntry> LearnedEntries =>
        learnedEntriesList;

    public IReadOnlyList<CategoryGroup> LearnedCategories =>
        learnedCategoriesRuntime;

    public event Action<NotebookEntry> OnWordLearned;
    public event Action<NotebookEntry> OnMelodyLearned;
    public event Action<WordCategory> OnCategoryUnlocked;

    [Serializable]
    public class CategoryGroup
    {
        public WordCategory category;
        public List<NotebookEntry> entries =
            new List<NotebookEntry>();

        public CategoryGroup(WordCategory category)
        {
            this.category = category;
            this.entries = new List<NotebookEntry>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 1. Intentamos obtener el objeto de guardado usando el tipo explícito [2]
        // 2. ¡Hemos quitado el punto y coma al final de la línea del 'if'!
        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out saveObject))
        {
            Debug.Log("SaveObject global encontrado con éxito.");
            
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar el NotebookSave global. Asegúrate de que está creado y registrado en los ajustes del Save Manager.", this);
        }
    }

    // ================= INTEGRACIÓN FEBUCCI TEXT ANIMATOR =================

    public void OnFebucciEventReceived(
        string eventName,
        string[] parameters)
    {
        if (!string.Equals(
                eventName,
                "LearnEntry",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (parameters == null || parameters.Length == 0)
            return;

        LearnEntryById(parameters[0]);
    }

    public void LearnEntryById(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
            return;

        NotebookEntry entry = FindEntryById(entryId);

        Debug.Log(
            $"[NotebookManager] LearnEntryById: {entryId} -> " +
            $"{(entry != null ? entry.name : "NULL")}"
        );

        if (entry != null)
        {
            LearnEntry(entry);
        }
        else
        {
            Debug.LogWarning(
                $"[NotebookManager] No se encontró ninguna entrada con la ID: {entryId}"
            );
        }
    }

    // ================= CORE LOGIC =================

    public void LearnEntry(NotebookEntry entry)
    {
        if (entry == null)
            return;

        bool alreadyLearned =
            learnedEntriesList.Contains(entry);

        if (!alreadyLearned)
        {
            if (entry.type == EntryType.Word &&
                entry.category.HasValue)
            {
                WordCategory category = entry.category.Value;

                if (!HasCategory(category))
                {
                    if (saveObject != null && saveObject.LearnedEntriesIds != null && saveObject.LearnedEntriesIds.Value != null)
        {
            if (!saveObject.LearnedEntriesIds.Value.Contains(entry.id.ToString()))
            {
                saveObject.LearnedEntriesIds.Value.Add(entry.id.ToString());

                SaveManager.SaveGame(); // Guarda los datos inmediatamente [3]
                Debug.Log("Word learned and saved");
            }
                }
            }

            
        }

            RebuildLearnedCategories();

            if (entry.type == EntryType.Word)
            {
                OnWordLearned?.Invoke(entry);
            }
            else if (entry.type == EntryType.Melody)
            {
                OnMelodyLearned?.Invoke(entry);
            }
        }

        if (!isLoading)
        {
            SaveEntry(entry);
        }
    }

    public void LearnCategory(WordCategory category)
    {
        if (HasCategory(category))
            return;

        CategoryGroup group = new CategoryGroup(category);
        learnedCategoriesRuntime.Add(group);

        RebuildLearnedCategories();

        if (!isLoading)
        {
            OnCategoryUnlocked?.Invoke(category);
        }
    }

    public bool HasLearned(NotebookEntry entry)
    {
        return entry != null &&
               learnedEntriesList.Contains(entry);
    }

    public bool HasCategory(WordCategory category)
    {
        return learnedCategoriesRuntime.Any(
            group =>
                group != null &&
                group.category == category
        );
    }

    public IReadOnlyList<NotebookEntry> GetEntriesByCategory(WordCategory category)
    {
        CategoryGroup group =
            learnedCategoriesRuntime.FirstOrDefault(
                group =>
                    group != null &&
                    group.category == category
            );

        if (group == null)
            return Array.Empty<NotebookEntry>();

        return group.entries
            .Where(entry => entry != null)
            .OrderBy(
                entry =>
                    entry.id != null
                        ? entry.id.GetLocalizedString()
                        : string.Empty,
                StringComparer.CurrentCultureIgnoreCase
            )
            .ToList();
    }

    private void RebuildLearnedCategories()
    {
        learnedCategoriesRuntime.Clear();

        var activeCategories = learnedEntriesList
            .Where(entry =>
                entry != null &&
                entry.type == EntryType.Word &&
                entry.category.HasValue
            )
            .Select(entry => entry.category.Value)
            .Distinct()
            .OrderBy(category => category)
            .ToList();

        foreach (WordCategory category in activeCategories)
        {
            CategoryGroup group =
                new CategoryGroup(category)
                {
                    entries = learnedEntriesList
                        .Where(entry =>
                            entry != null &&
                            entry.type == EntryType.Word &&
                            entry.category.HasValue &&
                            entry.category.Value == category
                        )
                        .OrderBy(
                            entry =>
                                entry.id != null
                                    ? entry.id.GetLocalizedString()
                                    : string.Empty,
                            StringComparer.CurrentCultureIgnoreCase
                        )
                        .ToList()
                };

            learnedCategoriesRuntime.Add(group);
        }
    }

    private NotebookEntry FindEntryById(string entryId)
    {
        return totalEntryList.FirstOrDefault(entry =>
            entry != null &&
            (
                string.Equals(
                    GetEntryId(entry),
                    entryId,
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    entry.name,
                    entryId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        );
    }

    private string GetEntryId(NotebookEntry entry)
    {
        if (entry == null || entry.id == null)
            return string.Empty;

        return entry.id.TableEntryReference.Key;
    }

    // ================= SAVE / LOAD =================

    private void TryGetSaveObject()
    {
        if (saveObject != null)
            return;

        if (!SaveManager.IsInitialized)
            return;

        if (!SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(
            out saveObject))
        {
            Debug.LogError(
                "[NotebookManager] Could not get NotebookSaveObject."
            );
        }
    }

    private void SaveEntry(NotebookEntry entry)
    {
        if (entry == null)
            return;

        TryGetSaveObject();

        if (saveObject == null)
        {
            Debug.LogError(
                "[NotebookManager] NotebookSaveObject is null."
            );

            return;
        }

        string entryId = GetEntryId(entry);

        if (string.IsNullOrEmpty(entryId))
        {
            Debug.LogError(
                $"[NotebookManager] Empty ID for entry '{entry.name}'."
            );

            return;
        }

        if (saveObject.LearnedEntriesIds == null)
        {
            Debug.LogError(
                "[NotebookManager] LearnedEntriesIds SaveValue is null."
            );

            return;
        }

        if (saveObject.LearnedEntriesIds.Value == null)
        {
            saveObject.LearnedEntriesIds.Value =
                new List<string>();
        }

        if (!saveObject.LearnedEntriesIds.Value.Contains(entryId))
        {
            saveObject.LearnedEntriesIds.Value.Add(entryId);
        }

        SaveManager.SaveGame();
    }

    public void SaveToDisk()
    {
        if (isLoading)
            return;

        TryGetSaveObject();

        if (saveObject == null)
            return;

        List<string> savedIds =
            saveObject.LearnedEntriesIds.Value;

        if (savedIds == null)
        {
            savedIds = new List<string>();
            saveObject.LearnedEntriesIds.Value = savedIds;
        }

        savedIds.Clear();

        foreach (NotebookEntry entry in learnedEntriesList)
        {
            string entryId = GetEntryId(entry);

            if (!string.IsNullOrEmpty(entryId))
            {
                savedIds.Add(entryId);
            }
        }

        SaveManager.SaveGame();
    }

    public void LoadFromDisk()
    {
        if (hasLoaded)
            return;

        TryGetSaveObject();

        if (saveObject == null)
            return;

        learnedEntriesList.Clear();
        learnedCategoriesRuntime.Clear();

        List<string> savedIds =
            saveObject.LearnedEntriesIds.Value;

        hasLoaded = true;

        if (savedIds == null || savedIds.Count == 0)
            return;

        isLoading = true;

        try
        {
            foreach (string savedId in savedIds)
            {
                if (string.IsNullOrEmpty(savedId))
                    continue;

                NotebookEntry entry =
                    FindEntryById(savedId);

                if (entry != null)
                {
                    LearnEntry(entry);
                }
            }

            RebuildLearnedCategories();
        }
        finally
        {
            isLoading = false;
        }
    }

    [ContextMenu("Debug/Delete Save File")]
    public void DeleteSaveFile()
    {
        TryGetSaveObject();

        if (saveObject != null)
        {
            saveObject.LearnedEntriesIds.ResetValue();
            SaveManager.SaveGame();
        }

        learnedEntriesList.Clear();
        learnedCategoriesRuntime.Clear();

        hasLoaded = true;
    }
}