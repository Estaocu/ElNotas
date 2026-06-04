using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private GameObject circlePivot;

    
    // Words for the player's sentence in the speech bubble
    private Word word1;
    private Word word2;

    void OnEnable()
    {

        currentWordList = totalWordList.GetWordsByCategory(currentCategory);

        foreach(WordUIBehaviour slot in slots)
        {
            int raw = slot.currentSlot - 2;
            int index = ((raw % currentWordList.Count) + currentWordList.Count) % currentWordList.Count;
            if (index <0 || index >= currentWordList.Count) continue;

            slot.word = currentWordList[index];
            slot.DisplayNewWord();
            //slot.SetWordText(word.displayName.GetLocalizedString());
            // slot.SetWordText(slot.currentSlot.ToString());
        }
    }

    

    private void Start()
    {
        if (totalWordList == null)
        {
            Debug.LogError("TotalWordList reference is missing!");
            return;
        }
        GetCategoryLenght();

        // read TWL's category list equal to current category DONE
        // Initially Slot 3 = Word 0. 
        // ALWAYS Slot N = Word N-3 . 
        // At the same time: current word ALWAYS equals N-3 + M (M=scroll) minScroll = 0; maxScroll = currentCategory.lenght DONE
        // NavigateWords negative --M; positive ++M DONE
    }
        //word in position 1 and 5, invisible, i'm setting you up in case any of you both are showing up next. how?
        //position 3 is the boss. marks the 0 point. check words for scroll +-2 and assign to extremes. keep them invisible
        //on navigatewords replay this function

    

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
            }
    }
    }

    public void SelectWord()
    {
        foreach(WordUIBehaviour slot in slots)
        {
            if (slot.currentSlot == 2)
            {
                Debug.Log("Word accepted: "+ slot.word.displayName.GetLocalizedString());
            }
        }
        
    }

    public void RotateWheel(int sign)
    {
        if (sign < 0)
        {
            circlePivot.transform.Rotate(0f, 0f, 30f); 
        }

        else
        {
            circlePivot.transform.Rotate(0f, 0f, -30f);
        }
    }

    public void GetCategoryLenght()
    {
        catLenght = totalWordList.GetCategoryLength(currentCategory);
    }

    public void OnCategoryChange()
    {
        GetCategoryLenght();
    }
}
