using System;
using System.Collections.Generic;
using System.Linq;
using CarterGames.Assets.SaveManager.Slots;
using CMF;
using TMPro;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class ConversationManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    public WordUIBehaviour[] slots;
    [SerializeField] private TotalWordList totalWordList;
    [SerializeField] private LearnedWords learnedWordList;
    
    [Tooltip("DEBUG: if true, the dictionary shows every word in TotalWordList instead of only the learned ones.")]
    [SerializeField] private bool debugUseTotalWordList = false;
    
    public WordCategory currentCategory;
    private IReadOnlyList<Word> currentWordList;
    private int catLength;

    public MelodyUI melodyUI;
    private NPC currentNpc;

    [SerializeField] private DialogueTextMaster npcText;
    [SerializeField] private PlayerBubbleWordBehaviour[] bubbleWords = new PlayerBubbleWordBehaviour[2];
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private string categoryTableName = "WordsNotebook";
    [SerializeField] private TextMeshProUGUI debugNum;
    [SerializeField] private WordUIBehaviour debugWord;

    private int centerWordIndex = 0;
    private int[] lastIndex;

    public void SetUIActive(bool desiredState)
    {
        gameObject.SetActive(desiredState);
    }

    private void Start()
    {
        if (debugUseTotalWordList && totalWordList == null)
            Debug.LogError("TotalWordList reference is missing!");
            
        if (!debugUseTotalWordList && learnedWordList == null)
            Debug.LogError("LearnedWords reference is missing!");
            
        UpdateCategoryLength();
    }

    private void OnEnable()
    {
        ClampCategoryToAvailable();
        List<WordCategory> available = GetAvailableCategories();
        lastIndex = new int[available.Count];
        debugNum.SetText(centerWordIndex.ToString());

        UpdateCategoryText();
        //AdaptSlotNumber();
        UpdateCategoryLength();
        AssignWordsToSlots();
        Debug.Log("CONVERSATION (re)/enabled");
        IndexToWord();
        npcText.transform.root.gameObject.SetActive(true);

        string txt = $"npc_{currentNpc.npcName.ToLower()}_answer0";
        npcText.AssignNewDialogue(txt);
    }

    private void ClampCategoryToAvailable()
    {
        List<WordCategory> available = GetAvailableCategories();
        if (available.Count == 0) gameObject.SetActive(false);
        
        if (!available.Contains(currentCategory) && available.Count > 0)
            currentCategory = available[0];
    }

    public void NavigateWords(InputAction.CallbackContext context)
    {
        if (context.performed){
        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        if (catLength <= 1) // 1
        {
            //PlayErrorSFX();
            return;
        }

        
        if (catLength < 4) // 2 o 3
        {
            int targetIndex = centerWordIndex - dpadValue;
            
            // Validar límites estrictos según el tamaño de la lista
            if (targetIndex < 0 || targetIndex >= catLength)
            {
                //PlayErrorSFX();
                return;
            }

            centerWordIndex = targetIndex;
        }
        else // 4 o +
        {
            // Movimiento circular (módulo genérico para evitar índices negativos en C#)
            centerWordIndex = (centerWordIndex - dpadValue + catLength) % catLength;

        }

        debugNum.SetText(centerWordIndex.ToString());
        IndexToWord();

        //UpdateSlotsVisuals(false);
    }
    }

        // foreach (WordUIBehaviour slot in slots)
        // {
        //     slot.RotateSlot(dpadValue);
            
        //     slot.RecalculateSlotPosition();
        //     slot.TeleportExtremes(dpadValue);
        //     AssignWordsToSlots();
        //     slot.UpdateVisuals();
        // } 


    public void NavigateCategory(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        List<WordCategory> available = GetAvailableCategories();
        if (available.Count < 2) return;

        int current = available.IndexOf(currentCategory);
        if (current < 0) current = 0;

        // Save last word hovered on the category before navigating to the next one
        lastIndex[current] = centerWordIndex;
        Debug.Log($"Category [{current}|{currentCategory}] Last index: {lastIndex[current]}");

        int next = ((current + dpadValue) % available.Count + available.Count) % available.Count;
        currentCategory = available[next];

        

        UpdateCategoryText();
        UpdateCategoryLength();
        //AdaptSlotNumber();
        AssignWordsToSlots();
        Debug.Log($"Category is now {currentCategory} | Length: {catLength}");
        
        centerWordIndex = lastIndex[next];
        debugNum.SetText(centerWordIndex.ToString());

        IndexToWord();

    }

    private List<WordCategory> GetAvailableCategories()
    {
        if (debugUseTotalWordList)
            return Enum.GetValues(typeof(WordCategory)).Cast<WordCategory>().ToList();

        if (learnedWordList == null) return new List<WordCategory>();

        return learnedWordList.LearnedCategories
            .Select(g => g.category)
            .OrderBy(c => (int)c)
            .ToList();
    }

    private void UpdateCategoryText()
    {
        if (categoryText == null) return;

        string id = $"category_{currentCategory}";
        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(categoryTableName, id);

        if (string.IsNullOrEmpty(localized) || localized.StartsWith("No translation"))
        {
            Debug.LogWarning($"No localized string for '{id}' in table '{categoryTableName}'");
            localized = currentCategory.ToString();
        }

        categoryText.SetText(localized);
    }

    public void SelectWord(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        for (int i = 0; i < bubbleWords.Length; i++)
            {
                if (bubbleWords[i].full) continue;

                bubbleWords[i].PlaceWord(debugWord.word);

                // If it was the last empty bubble, the sentence is complete
                if (i == bubbleWords.Length - 1)
                    FindAnswer();

                return;
            }

        // foreach (WordUIBehaviour slot in slots)
        // {
        //     if (slot.currentSlot != 2) continue;

        //     for (int i = 0; i < bubbleWords.Length; i++)
        //     {
        //         if (bubbleWords[i].full) continue;

        //         bubbleWords[i].PlaceWord(slot.word);

        //         // If it was the last empty bubble, the sentence is complete
        //         if (i == bubbleWords.Length - 1)
        //             FindAnswer();

        //         return;
        //     }
        // }
    }

    public void EraseWord(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        for (int i = bubbleWords.Length - 1; i >= 0; i--)
        {
            if (!bubbleWords[i].full) continue;
            bubbleWords[i].EraseWord();
            return;
        }
    }

    public void FindAnswer()
    {
        npcText.trigger.ToggleInConv(true);

        string a = bubbleWords[0].word.name.ToLower();
        string b = bubbleWords[1].word.name.ToLower();
        string npc = currentNpc.npcName.ToLower();
        
        // Alphabetical sorting
        if (string.CompareOrdinal(a, b) > 0) (a, b) = (b, a); 

        string primaryId = $"npc_{npc}_{a}{b}";
        string swappedId  = $"npc_{npc}_{b}{a}";

        Debug.Log("Finding dialogue: " + primaryId);

        npcText.AssignNewDialogue(primaryId, swappedId);
        

        ActionMapsManager.SetActiveMaps(DefaultActionMap.Text);

    }

    public void UpdateCategoryLength()
    {
        currentWordList = GetActiveWordsByCategory(currentCategory);
        catLength = currentWordList != null ? currentWordList.Count : 0;
    }

    private IReadOnlyList<Word> GetActiveWordsByCategory(WordCategory category)
    {
        if (debugUseTotalWordList)
        {
            return totalWordList != null ? totalWordList.GetWordsByCategory(category) : new List<Word>();
        }

        return learnedWordList != null ? learnedWordList.GetWordsByCategory(category) : new List<Word>();
    }

    public void GetCategoryLenght()
    {
        catLength = debugUseTotalWordList
            ? totalWordList.GetCategoryLength(currentCategory)
            : (learnedWordList != null ? learnedWordList.GetCategoryLength(currentCategory) : 0);
    }

    private void AssignWordsToSlots()
    {
        currentWordList = GetActiveWordsByCategory(currentCategory);
        if (currentWordList == null || currentWordList.Count == 0) return;

        // Synchronize catLength directly with active list count to prevent out-of-bounds indexing
        catLength = currentWordList.Count;
    }

    public void SetNewNPC(NPC newNpc)
    {
        currentNpc = newNpc;
    }

    private void IndexToWord()
    {
        if (currentWordList == null || currentWordList.Count == 0)
        {
            Debug.LogWarning("IndexToWord skipped: currentWordList is null or empty.");
            return;
        }

        // Ensure centerWordIndex stays safely within currentWordList bounds
        int index = ((centerWordIndex % currentWordList.Count) + currentWordList.Count) % currentWordList.Count;

        Word selectedWord = currentWordList[index];

        if (selectedWord == null)
        {
            Debug.LogError($"Word at index {index} in category {currentCategory} is null!");
            return;
        }

        debugWord.DisplayNewWord(selectedWord);
}

    public void ReturnConversation()
        {

        foreach (PlayerBubbleWordBehaviour bubble in bubbleWords)
        {
            bubble.EraseWord();
        }

        ActionMapsManager.SetActiveMaps(DefaultActionMap.Conversation);
        string txt = $"npc_{currentNpc.npcName.ToLower()}_answer1";

        npcText.AssignNewDialogue(txt);
        }

            


        
}