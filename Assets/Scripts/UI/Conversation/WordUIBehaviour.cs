using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using System.Linq;


public class WordUIBehaviour : MonoBehaviour
{
    
    public Image bgWord;
    public Image bgMelody;
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI melodyString;
    // public bool isSelected; REDUNDANTE, SI ES SLOT 3 (o 2 segun como cuentes) ESTÁ SELECTED SIEMPRE
    public int currentSlot;
    public Word word;

    void Start()
    {
        UpdateVisuals();
        DisplayNewWord();
    }

    public void SetWordText(LocalizedString sourceText)
    {
        wordText.SetText(sourceText.GetLocalizedString());
    }

    public void SetMelodyString(string sourceText)
    {
        melodyString.SetText(sourceText);
    }

    public void HideMelody()
    {
        bgMelody.GetComponent<Image>().enabled = false;
        melodyString.enabled = false;
    }
    public void ShowMelody()
    {
        bgMelody.GetComponent<Image>().enabled = true;
        melodyString.enabled = true;
    }
    public void Rotate30(int dpadValue = 0)
    {
        if (dpadValue == 0) return;

        if (dpadValue < 0) gameObject.transform.Rotate(0f, 0f, 30f);
        else gameObject.transform.Rotate(0f, 0f, -30f);
    }

    public void OnWheelRotated(int dpadValue)
    {
        currentSlot = (currentSlot + dpadValue);
        UpdateVisuals(dpadValue);
        //TeleportExtremes();
    }

    public void UpdateVisuals(int dpadValue = 0)
    {
        //SetWordText(currentSlot.ToString());
        switch (currentSlot)
        {
            case 0:
            case 4:
            bgWord.color = Color.black;
            //bgWord.enabled = false;
            //wordText.enabled = false;
            HideMelody();    
            DisplayNewWord();

            break;

            case 1: 
            case 3:  
            bgWord.enabled = true;
            wordText.enabled = true;
            bgWord.color = Color.blue;
            HideMelody();     
            break;

            case 2:
            bgWord.color = Color.cyan;
            ShowMelody(); 
            break;

            case 5:
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
            currentSlot = 0;
            break;

            case -1:
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
            currentSlot = 4;
            break;

            default:
            Debug.Log("Default in Switch!");
            break;
        }
    }

    public void TeleportExtremes()
    {
        if (currentSlot != 0 || currentSlot != 4) return;
        if (currentSlot == 0)
        {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
        }
        else
        {
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
        }
    }

    public void DisplayNewWord()
    {
        SetWordText(word.displayName);
        string melodyText = string.Concat(word.melody.Select(n => ((int)n + 1).ToString()));
        melodyString.SetText(melodyText);
    }
}
