using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Splines.ExtrusionShapes;

public class DictionaryUIManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    public WordUIBehaviour[] slots;
    [SerializeField] private TotalWordList totalWordList;
    public WordCategory currentCategory = WordCategory.Other;
    private IReadOnlyList<Word> currentWordList;
    // private int scroll = 0;
    private int catLenght;
    //[SerializeField] private GameObject circlePivot;

    public MelodyUI melodyUI;

    public NPC currentNpc;

    [SerializeField] private DialogueTextMaster npcText;

    
    // Words for the player's sentence in the speech bubble
    [SerializeField] private PlayerBubbleWordBehaviour[] bubbleWords = new PlayerBubbleWordBehaviour[2];

    void OnEnable()
    {
        ResetUI();

        currentWordList = totalWordList.GetWordsByCategory(currentCategory);

        foreach(WordUIBehaviour slot in slots)
        {
            int raw = slot.currentSlot - 2;
            int index = ((raw % currentWordList.Count) + currentWordList.Count) % currentWordList.Count;
            if (index <0 || index >= currentWordList.Count) continue;

            slot.word = currentWordList[index];
            slot.DisplayNewWord();

            if (slot.currentSlot == 3)
                {
                    melodyUI.ChangeMelodyDisplayed(slot.word.melody);
                    melodyUI.SetYValues();
                }
            //slot.SetWordText(word.displayName.GetLocalizedString());
            // slot.SetWordText(slot.currentSlot.ToString());
        }
    }

    public void ResetUI()
    {
        
    }

    

    private void Start()
    {
        if (totalWordList == null)
        {
            Debug.LogError("TotalWordList reference is missing!");
            return;
        }
        GetCategoryLenght();
    }
    public void NavigateWords(InputAction.CallbackContext context)
    {
        if (context.performed)
    {
        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        foreach(WordUIBehaviour slot in slots)
            {
                // slot.SetExtremesWords();
                slot.Rotate30(dpadValue);
                slot.OnWheelRotated(dpadValue);
                if (slot.currentSlot == 3)
                {
                    melodyUI.ChangeMelodyDisplayed(slot.word.melody);
                    melodyUI.SetYValues();
                }
            }
    }
    }

    public void SelectWord(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        foreach (WordUIBehaviour slot in slots)
        {
            if (slot.currentSlot != 2) continue;

            for (int i = 0; i < bubbleWords.Length; i++)
            {
                if (bubbleWords[i].full) continue;

                bubbleWords[i].PlaceWord(slot.word);

                // ¿Era la última burbuja libre? Entonces ya tenemos frase completa.
                if (i == bubbleWords.Length - 1)
                    FindDialogue();

                return;
            }
        }
}

    public void EraseWord(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            for (int i = bubbleWords.Length - 1; i >= 0; i--)
        {
            if (!bubbleWords[i].full) continue;
            bubbleWords[i].EraseWord();
            return;
        }
        }
        
    }

    public void FindDialogue()
    {
        
        string a = bubbleWords[0].word.name.ToLower();
        string b = bubbleWords[1].word.name.ToLower();
        string npc = currentNpc.npcName.ToLower();
        if (string.CompareOrdinal(a, b) > 0) (a, b) = (b, a); // alfabético

        string primaryId = $"npc_{npc}_{a}{b}";
        string swappedId  = $"npc_{npc}_{b}{a}";

        Debug.Log("Finding dialogue: " + primaryId);

        npcText.AssignNewDialogue(primaryId, swappedId);
    }

    public void GetCategoryLenght()
    {
        catLenght = totalWordList.GetCategoryLength(currentCategory);
    }

    public void OnCategoryChange()
    {
        GetCategoryLenght();
    }

    public void OnDisable()
    {
        currentNpc = null;
    }
}
