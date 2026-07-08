using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

    private const string SaveFileName = "learned_words.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private bool isLoading;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        ResetProgress();
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

    public string ToJson()
    {
        var save = new LearnedWordsSave
        {
            version = 1,
            wordIDs = learnedWordList.Select(w => w.wordID).ToList()
        };
        return JsonUtility.ToJson(save, prettyPrint: true);
    }

    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json) || totalWords == null) return;

        var save = JsonUtility.FromJson<LearnedWordsSave>(json);
        if (save == null || save.wordIDs == null) return;

        ResetProgress();

        isLoading = true;
        try
        {
            foreach (string id in save.wordIDs)
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

    public void SaveToDisk()
    {
        try
        {
            File.WriteAllText(SavePath, ToJson());
        }
        catch (Exception e)
        {
            Debug.LogError($"LearnedWords: failed to save to '{SavePath}'. {e.Message}");
        }
    }

    public void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            LoadFromJson(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"LearnedWords: failed to load from '{SavePath}'. {e.Message}");
        }
    }

    [ContextMenu("Debug/Delete Save File")]
    private void DeleteSaveFile()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        ResetProgress();
    }
}
