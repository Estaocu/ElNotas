using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class NotebookDictionarySlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI wordTxt;
    [SerializeField] private MelodyUI score;

    // Este método se ejecuta AUTOMÁTICAMENTE en el Editor de Unity
    private void OnValidate()
    {
        if (wordTxt == null)
        {
            wordTxt = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        
        if (score == null)
        {
            score = GetComponentInChildren<MelodyUI>(true);
        }
    }

    public void DisplayScore()
    {
        score.ShowMelody();
        score.SetYValues();
    }

    public void SetMelody(notesEnum[] targetMelody)
    {
        if (score != null)
        {
            score.ChangeMelodyDisplayed(targetMelody);
            DisplayScore();
        }
    }

    public void SetText(LocalizedString targetLocString)
    {
        if (wordTxt != null && targetLocString != null)
        {
            wordTxt.SetText(targetLocString.GetLocalizedString());
        }
    }
}
