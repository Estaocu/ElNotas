using System;
using System.Collections.Generic;
using System.Linq;
using CarterGames.Assets.SaveManager;
using Save;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LearnedLearnedEntriesIds",
    menuName = "ScriptableObjects/Learned Notebook Entries",
    order = 3
)]
public class LearnedLearnedEntriesIds : ScriptableObject
{
    [Header("Total Notebook Entries")]
    [SerializeField]
    private List<NotebookEntry> totalEntryList =
        new List<NotebookEntry>();

    [Header("Learned Notebook Entries")]
    [SerializeField]
    private List<NotebookEntry> learnedEntriesList =
        new List<NotebookEntry>();

    [Header("Derived Word Categories")]
    [SerializeField]
    private List<CategoryGroup> categories =
        new List<CategoryGroup>();

    [SerializeField]
    private List<CategoryGroup>
        learnedCategoriesRuntime =
        new List<CategoryGroup>();

    private bool isLoading;

    public IReadOnlyList<NotebookEntry> TotalEntries =>
        totalEntryList;

    public IReadOnlyList<NotebookEntry> LearnedEntries =>
        learnedEntriesList;

    public IReadOnlyList<CategoryGroup> LearnedCategories =>
        learnedCategoriesRuntime;

    public event Action<NotebookEntry> OnWordLearned;
    public event Action<NotebookEntry> OnMelodyLearned;
    public event Action<WordCategory> OnCategoryUnlocked;
    public event Action<ChapterType> OnChapterUnlocked;

    [Serializable]
    public class CategoryGroup
    {
        public WordCategory category;

        public List<NotebookEntry> entries =
            new List<NotebookEntry>();

        public CategoryGroup(
            WordCategory category)
        {
            this.category = category;
            this.entries = new List<NotebookEntry>();
        }
    }

    private void OnValidate()
    {
        CleanTotalEntries();
        SyncCategories();
    }

    private void CleanTotalEntries()
    {
        if (totalEntryList == null)
        {
            totalEntryList =
                new List<NotebookEntry>();

            return;
        }

        // Sort assigned entries while keeping empty slots at the end.
        List<NotebookEntry> validEntries =
            totalEntryList
                .Where(entry => entry != null)
                .OrderBy(GetEntryId)
                .ToList();

        List<NotebookEntry> nullEntries =
            totalEntryList
                .Where(entry => entry == null)
                .ToList();

        totalEntryList =
            validEntries
                .Concat(nullEntries)
                .ToList();
    }

    private void SyncCategories()
    {
        if (categories == null)
        {
            categories =
                new List<CategoryGroup>();
        }

        List<WordCategory> activeCategories =
            totalEntryList
                .Where(entry =>
                    entry != null &&
                    entry.type == EntryType.Word &&
                    entry.category.HasValue
                )
                .Select(entry =>
                    entry.category.Value
                )
                .Distinct()
                .OrderBy(category => category)
                .ToList();

        categories.RemoveAll(
            group =>
                group == null ||
                !activeCategories.Contains(
                    group.category
                )
        );

        foreach (WordCategory category in activeCategories)
        {
            CategoryGroup group =
                categories.FirstOrDefault(
                    current =>
                        current != null &&
                        current.category == category
                );

            if (group == null)
            {
                group =
                    new CategoryGroup(category);

                categories.Add(group);
            }

            group.entries =
                totalEntryList
                    .Where(entry =>
                        entry != null &&
                        entry.type == EntryType.Word &&
                        entry.category.HasValue &&
                        entry.category.Value == category
                    )
                    .OrderBy(GetEntryId)
                    .ToList();
        }

        categories =
            categories
                .OrderBy(group => group.category)
                .ToList();
    }

    public void LearnEntry(
        NotebookEntry entry)
    {
        if (entry == null)
            return;

        if (learnedEntriesList.Contains(entry))
            return;

        bool isNewCategory = false;

        if (entry.type == EntryType.Word &&
            entry.category.HasValue)
        {
            WordCategory category =
                entry.category.Value;

            isNewCategory =
                !HasCategory(category);

            if (isNewCategory)
            {
                LearnCategory(category);
            }
        }

        learnedEntriesList.Add(entry);

        RebuildLearnedCategories();

        if (entry.type == EntryType.Word)
        {
            OnWordLearned?.Invoke(entry);
        }
        else if (entry.type == EntryType.Melody)
        {
            OnMelodyLearned?.Invoke(entry);
        }

        if (!isLoading)
        {
            SaveToDisk();
        }
    }

    public void LearnCategory(
        WordCategory category)
    {
        if (HasCategory(category))
            return;

        CategoryGroup group =
            categories.FirstOrDefault(
                current =>
                    current != null &&
                    current.category == category
            );

        if (group == null)
            return;

        List<NotebookEntry> learnedWords =
            learnedEntriesList
                .Where(entry =>
                    entry != null &&
                    entry.type == EntryType.Word &&
                    entry.category.HasValue &&
                    entry.category.Value == category
                )
                .ToList();

        CategoryGroup learnedCategory =
            new CategoryGroup(category);

        learnedCategory.entries =
            learnedWords;

        learnedCategoriesRuntime.Add(
            learnedCategory
        );

        learnedCategoriesRuntime =
            learnedCategoriesRuntime
                .OrderBy(group =>
                    group.category)
                .ToList();

        if (!isLoading)
        {
            OnCategoryUnlocked?.Invoke(
                category
            );
        }
    }

    public bool HasLearned(
        NotebookEntry entry)
    {
        if (entry == null)
            return false;

        return learnedEntriesList.Contains(entry);
    }

    public bool HasLearned(
        string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
            return false;

        return learnedEntriesList.Any(
            entry =>
                entry != null &&
                string.Equals(
                    GetEntryId(entry),
                    entryId,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }

    public bool HasCategory(
        WordCategory category)
    {
        return learnedCategoriesRuntime.Any(
            group =>
                group != null &&
                group.category == category
        );
    }

    public int GetCategoryLength(
        WordCategory category)
    {
        CategoryGroup group =
            learnedCategoriesRuntime.FirstOrDefault(
                current =>
                    current != null &&
                    current.category == category
            );

        return group != null
            ? group.entries.Count
            : 0;
    }

    public IReadOnlyList<NotebookEntry>
        GetEntriesByCategory(
            WordCategory category)
    {
        CategoryGroup group =
            learnedCategoriesRuntime.FirstOrDefault(
                current =>
                    current != null &&
                    current.category == category
            );

        if (group == null)
            return Array.Empty<NotebookEntry>();

        return group.entries
            .Where(entry => entry != null)
            .OrderBy(
                GetLocalizedEntryName,
                StringComparer.CurrentCultureIgnoreCase
            )
            .ToList();
    }

    public void ResetProgress()
    {
        learnedEntriesList.Clear();
        learnedCategoriesRuntime.Clear();
    }

    public void SaveToDisk()
    {
        if (isLoading)
            return;

        if (!SaveManager.TryGetGlobalSaveObject<
                NotebookSaveObject>(
                out NotebookSaveObject saveObject))
        {
            return;
        }

        List<string> entryIds =
            learnedEntriesList
                .Where(entry => entry != null)
                .Select(GetEntryId)
                .Where(id =>
                    !string.IsNullOrEmpty(id))
                .ToList();

        saveObject.LearnedEntriesIds.Value =
            entryIds;

        SaveManager.SaveGame();
    }

    public void LoadFromDisk()
    {
        if (!SaveManager.TryGetGlobalSaveObject<
                NotebookSaveObject>(
                out NotebookSaveObject saveObject))
        {
            return;
        }

        ResetProgress();

        List<string> savedIds =
            saveObject.LearnedEntriesIds.Value;

        if (savedIds == null ||
            savedIds.Count == 0)
        {
            return;
        }

        isLoading = true;

        try
        {
            foreach (string savedId in savedIds)
            {
                if (string.IsNullOrEmpty(savedId))
                    continue;

                NotebookEntry entry =
                    FindEntryById(savedId);

                if (entry == null)
                    continue;

                LearnEntry(entry);
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
        if (SaveManager.TryGetGlobalSaveObject<
                NotebookSaveObject>(
                out NotebookSaveObject saveObject))
        {
            saveObject.LearnedEntriesIds.ResetValue();
            SaveManager.SaveGame();
        }

        ResetProgress();
    }

    public void UnlockChapter(
        ChapterType chapterType)
    {
        OnChapterUnlocked?.Invoke(
            chapterType
        );
    }

    private NotebookEntry FindEntryById(
        string entryId)
    {
        return totalEntryList.FirstOrDefault(
            entry =>
                entry != null &&
                string.Equals(
                    GetEntryId(entry),
                    entryId,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }

    private void RebuildLearnedCategories()
    {
        learnedCategoriesRuntime.Clear();

        List<WordCategory> activeCategories =
            learnedEntriesList
                .Where(entry =>
                    entry != null &&
                    entry.type == EntryType.Word &&
                    entry.category.HasValue
                )
                .Select(entry =>
                    entry.category.Value
                )
                .Distinct()
                .OrderBy(category => category)
                .ToList();

        foreach (WordCategory category in activeCategories)
        {
            CategoryGroup group =
                new CategoryGroup(category);

            group.entries =
                learnedEntriesList
                    .Where(entry =>
                        entry != null &&
                        entry.type == EntryType.Word &&
                        entry.category.HasValue &&
                        entry.category.Value == category
                    )
                    .OrderBy(
                        GetLocalizedEntryName,
                        StringComparer.CurrentCultureIgnoreCase
                    )
                    .ToList();

            learnedCategoriesRuntime.Add(
                group
            );
        }
    }

    private string GetEntryId(
        NotebookEntry entry)
    {
        if (entry == null ||
            entry.id == null)
        {
            return string.Empty;
        }

        return entry.id.TableEntryReference.Key;
    }

    private string GetLocalizedEntryName(
        NotebookEntry entry)
    {
        if (entry == null ||
            entry.id == null)
        {
            return string.Empty;
        }

        return entry.id.GetLocalizedString();
    }
}