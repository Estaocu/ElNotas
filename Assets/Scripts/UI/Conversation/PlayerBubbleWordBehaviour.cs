using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class PlayerBubbleWordBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI wordName;
    [SerializeField] private MelodyUI notesDisplay;
    [SerializeField] private Image wordBG;
    public Word word;
    public bool full;

    void OnEnable()
    {
        EraseWord();
        notesDisplay.HideMelody();
    }

    public void PlaceWord(Word newWord)
    {
        full = true;
        notesDisplay.enabled = true;
        word = newWord;
        notesDisplay.ShowMelody();
        notesDisplay.ChangeMelodyDisplayed(word.melody);
        notesDisplay.SetYValues();
        wordName.SetText(word.displayName.GetLocalizedString());
        
    }

    public void EraseWord()
    {
        full = false;
        notesDisplay.HideMelody();
        word = null;
        wordName.SetText("");
        notesDisplay.enabled = false;
    }


}
