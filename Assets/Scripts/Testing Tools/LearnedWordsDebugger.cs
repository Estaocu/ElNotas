using UnityEngine;
using UnityEngine.InputSystem;

public class LearnedWordsDebugger : MonoBehaviour
{
    [SerializeField] LearnedWords learnedWords;

    [Header("Word slots — tecla numérica = índice")]
    [SerializeField] Word word1;   // tecla 1
    [SerializeField] Word word2;   // tecla 2
    [SerializeField] Word word3;   // tecla 3
    [SerializeField] Word word4;
    [SerializeField] Word word5;
    [SerializeField] Word word6;
    [SerializeField] Word word7;
    [SerializeField] Word word8;
    [SerializeField] Word word9;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || learnedWords == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Try(word1);
        if (kb.digit2Key.wasPressedThisFrame) Try(word2);
        if (kb.digit3Key.wasPressedThisFrame) Try(word3);
        if (kb.digit4Key.wasPressedThisFrame) Try(word4);
        if (kb.digit5Key.wasPressedThisFrame) Try(word5);
        if (kb.digit6Key.wasPressedThisFrame) Try(word6);
        if (kb.digit7Key.wasPressedThisFrame) Try(word7);
        if (kb.digit8Key.wasPressedThisFrame) Try(word8);
        if (kb.digit9Key.wasPressedThisFrame) Try(word9);

        if (kb.digit0Key.wasPressedThisFrame)
            Debug.Log($"[LearnedWords] Total aprendidas: {learnedWords.All.Count} → {string.Join(", ", learnedWords.All)}");
    }

    void Try(Word w)
    {
        if (w == null) { Debug.LogWarning("[LearnedWords] Slot vacío"); return; }
        bool added = learnedWords.Learn(w);
        Debug.Log(added
            ? $"[LearnedWords] Aprendida: {w.wordID}"
            : $"[LearnedWords] Ya estaba aprendida: {w.wordID}");
    }
}