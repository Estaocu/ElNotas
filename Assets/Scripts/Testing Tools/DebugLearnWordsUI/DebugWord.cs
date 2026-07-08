using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DebugWord : MonoBehaviour
{
    private Word word;
    private LearnedWords learnedWords;
    private Button button;
    private TMP_Text label;

    public void Init(Word word, LearnedWords learnedWords)
    {
        this.word = word;
        this.learnedWords = learnedWords;

        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = word != null ? word.wordID : "<null>";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        RefreshState();
    }

    private void OnClicked()
    {
        if (learnedWords == null || word == null) return;

        learnedWords.LearnWord(word);
        RefreshState();
    }

    private void RefreshState()
    {
        if (button == null || learnedWords == null || word == null) return;

        bool already = learnedWords.HasLearned(word);
        button.interactable = !already;

        if (label != null)
            label.text = already ? $"{word.wordID} OK" : word.wordID;
    }
}
