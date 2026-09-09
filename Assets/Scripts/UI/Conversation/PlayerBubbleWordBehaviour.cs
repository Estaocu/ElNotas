using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBubbleWordBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI wordName;
    [SerializeField] private MelodyUI notesDisplay;
    [SerializeField] private Image wordBG;
    public NotebookEntry entry;
    public bool full;

    void OnEnable()
    {
        EraseWord();
        notesDisplay.HideMelody();
    }

    public void PlaceWord(NotebookEntry newWord)
    {
        full = true;
        notesDisplay.enabled = true;
        entry = newWord;
        notesDisplay.ShowMelody();
        notesDisplay.ChangeMelodyDisplayed(entry.melody);
        notesDisplay.SetYValues();
        wordName.SetText(entry.id.GetLocalizedString());
        
    }

    public void EraseWord()
    {
        full = false;
        notesDisplay.HideMelody();
        entry = null;
        wordName.SetText("");
        notesDisplay.enabled = false;
    }


}
