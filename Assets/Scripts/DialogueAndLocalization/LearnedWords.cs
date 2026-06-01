using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LearnedWords", menuName = "ScriptableObjects/LearnedWords")]
public class LearnedWords : ScriptableObject
{
    const string PrefsKey = "LearnedWords";
    const char Sep = ';';

    HashSet<string> learned;

    public IReadOnlyCollection<string> All => learned;

    void OnEnable()
    {
        learned = new HashSet<string>();
        var raw = PlayerPrefs.GetString(PrefsKey, "");
        if (raw.Length == 0) return;
        foreach (var id in raw.Split(Sep))
            if (id.Length > 0) learned.Add(id);
    }

    public bool Has(Word w) => w != null && learned.Contains(w.wordID);
    public bool Has(string id) => learned.Contains(id);

    public bool Learn(Word w)
    {
        if (w == null || string.IsNullOrEmpty(w.wordID)) return false;
        if (!learned.Add(w.wordID)) return false;
        PlayerPrefs.SetString(PrefsKey, string.Join(Sep.ToString(), learned));
        PlayerPrefs.Save();
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Saved Words")]
    void ClearSaved() { learned.Clear(); PlayerPrefs.DeleteKey(PrefsKey); PlayerPrefs.Save(); }
#endif
}