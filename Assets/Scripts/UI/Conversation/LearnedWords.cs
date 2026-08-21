using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarterGames.Assets.SaveManager;
using Save;

[CreateAssetMenu(fileName = "LearnedWords", menuName = "ScriptableObjects/Learned Words", order = 3)]
public class LearnedWords : ScriptableObject
{
    [SerializeField] private TotalWordList totalWords;

    [SerializeField] private List<Word> learnedWordList = new List<Word>();

    [ListToGroup("category")]
    [SerializeField] private List<TotalWordList.CategoryGroup> learnedCategories = new List<TotalWordList.CategoryGroup>();

    public IReadOnlyList<Word> LearnedWordsList => learnedWordList;
    public IReadOnlyList<TotalWordList.CategoryGroup> LearnedCategories => learnedCategories;

    public event Action<Word> OnWordLearned;
    public event Action<WordCategory> OnCategoryLearned;

    private bool isLoading;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        
        ResetProgress();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
    }

    private void OnSaveLoaded()
    {
        // Se ejecuta automáticamente en cuanto SaveManager termina de cargar
        LoadFromDisk();
    }

    public void LearnWord(Word word)
    {
        if (word == null) return;

        if (learnedWordList.Contains(word)) return;

        Word source = totalWords != null
            ? totalWords.TotalWords.FirstOrDefault(w => w == word || w.wordID == word.wordID)
            : word;

        if (source == null)
        {
            Debug.LogWarning($"LearnedWords: word '{word.wordID}' not found in TotalWordList.");
            return;
        }

        bool categoryKnown = learnedCategories.Any(g => g.category == source.category);
        if (!categoryKnown) LearnCategory(source.category);

        learnedWordList.Add(source);
        learnedWordList = learnedWordList.OrderBy(w => w.wordID).ToList();

        var group = learnedCategories.First(g => g.category == source.category);
        group.words.Add(source);
        group.words = group.words.OrderBy(w => w.wordID).ToList();

        if (!isLoading)
        {
            OnWordLearned?.Invoke(source);
            SaveToDisk();
        }
    }

    public void LearnCategory(WordCategory category)
    {
        if (learnedCategories.Any(g => g.category == category)) return;

        learnedCategories.Add(new TotalWordList.CategoryGroup(category));
        learnedCategories = learnedCategories.OrderBy(g => g.category).ToList();

        if (!isLoading)
            OnCategoryLearned?.Invoke(category);
    }

    public bool HasLearned(Word word)
    {
        return word != null && learnedWordList.Contains(word);
    }

    public bool HasCategory(WordCategory category)
    {
        return learnedCategories.Any(g => g.category == category);
    }

    public int GetCategoryLength(WordCategory category)
    {
        var group = learnedCategories.FirstOrDefault(g => g.category == category);
        return group != null ? group.words.Count : 0;
    }

    public IReadOnlyList<Word> GetWordsByCategory(WordCategory category)
    {
        var group = learnedCategories.FirstOrDefault(g => g.category == category);
        if (group == null) return new List<Word>();

        return group.words
            .OrderBy(w => w.displayName.GetLocalizedString(), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void ResetProgress()
    {
        learnedWordList.Clear();
        learnedCategories.Clear();
    }

    public void SaveToDisk()
    {
        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out var saveObj))
        {
            saveObj.LearnedWordIDs.Value = learnedWordList.Select(w => w.wordID).ToList();
            SaveManager.SaveGame();
        }
    }

    public void LoadFromDisk()
    {
        // Reseteamos el estado actual antes de volcar lo que haya en SaveManager
        ResetProgress();

        if (totalWords == null) return;

        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out var saveObj))
        {
            List<string> savedIDs = saveObj.LearnedWordIDs.Value;

            // Si el Save Editor está vacío (0 palabras), la lista en ejecución se quedará vacía (0 palabras)
            if (savedIDs == null || savedIDs.Count == 0) return;

            isLoading = true;
            try
            {
                foreach (string id in savedIDs)
                {
                    Word w = totalWords.TotalWords.FirstOrDefault(x => x != null && x.wordID == id);
                    if (w != null) LearnWord(w);
                }
            }
            finally
            {
                isLoading = false;
            }
        }
    }

    [ContextMenu("Debug/Delete Save File")]
    private void DeleteSaveFile()
    {
        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out var saveObj))
        {
            saveObj.LearnedWordIDs.ResetValue();
            SaveManager.SaveGame();
        }
        ResetProgress();
    }
}