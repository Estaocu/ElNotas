using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class DictionaryUIManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    public WordUIBehaviour[] slots;
    [SerializeField] private TotalWordList totalWordList;
    [SerializeField] private LearnedWords learnedWordList;
    [Tooltip("DEBUG: if true, the dictionary shows every word in TotalWordList instead of only the learned ones.")]
    [SerializeField] private bool debugUseTotalWordList = false;
    public WordCategory currentCategory;
    private IReadOnlyList<Word> currentWordList;
    // private int scroll = 0;
    private int catLenght;
    //[SerializeField] private GameObject circlePivot;

    public MelodyUI melodyUI;

    public NPC currentNpc;

    [SerializeField] private DialogueTextMaster npcText;
    // Words for the player's sentence in the speech bubble
    [SerializeField] private PlayerBubbleWordBehaviour[] bubbleWords = new PlayerBubbleWordBehaviour[2];

    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private string categoryTableName = "WordsNotebook";

    void OnEnable()
    {
        ClampCategoryToAvailable();
        UpdateCategoryText();
        AdaptSlotNumber();
        RefreshSlotsForCurrentCategory();
    }

    private void ClampCategoryToAvailable()
    {
        List<WordCategory> available = GetAvailableCategories();
        if (available.Count == 0) gameObject.SetActive(false);
        if (!available.Contains(currentCategory))
            currentCategory = available[0];
    }

    private void RefreshSlotsForCurrentCategory()
    {
        currentWordList = GetActiveWordsByCategory(currentCategory);
        if (currentWordList == null || currentWordList.Count == 0) return;

        foreach (WordUIBehaviour slot in slots)
        {
            int raw = slot.currentSlot - 2;
            int index = ((raw % currentWordList.Count) + currentWordList.Count) % currentWordList.Count;
            if (index < 0 || index >= currentWordList.Count) continue;

            slot.word = currentWordList[index];
            slot.DisplayNewWord();

            if (slot.currentSlot == 3)
            {
                melodyUI.ChangeMelodyDisplayed(slot.word.melody);
                melodyUI.SetYValues();
            }
        }
        
    }

    

    private void Start()
    {
        if (debugUseTotalWordList && totalWordList == null)
        {
            Debug.LogError("TotalWordList reference is missing!");
            return;
        }
        if (!debugUseTotalWordList && learnedWordList == null)
        {
            Debug.LogError("LearnedWords reference is missing!");
            return;
        }
        GetCategoryLenght();

    }
    public void NavigateWords(InputAction.CallbackContext context)
    {
        if (!context.performed || catLenght == 1) return;

        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        if (catLenght == 2)
        {
            Debug.Log("Category is 2 items long. Navigating: " + dpadValue);
            int m =+ dpadValue;
            foreach (WordUIBehaviour slot in slots)
            {
                if (slot.currentSlot == 1 || slot.currentSlot == 3 ||slot.currentSlot == 4) slot.ToggleVisibility(false);
                if (slot.currentSlot == 2 && slot == slots[2] && dpadValue == -1)
                {
                    Debug.Log("Tried to go UP from slot 2");
                    return;
                } 
                

                if (slot.currentSlot == 2 && slot == slots[3] && dpadValue == 1)
                {
                    Debug.Log("Tried to go DOWN from slot 3");
                    return;
                }
            }

        }

        if (catLenght == 3)
        {
            foreach(WordUIBehaviour slot in slots)
            {
                if (slot.currentSlot == 1 || slot.currentSlot == 4) slot.ToggleVisibility(false);
                if (slot.currentSlot == 2 && slot == slots[2] && dpadValue == -1)

                {
                    Debug.Log("Tried to go UP from slot 2");
                    return;
                }

                if (slot.currentSlot == 4 && slot == slots[3] && dpadValue == 1)
                {
                    Debug.Log("Tried to go DOWN from slot 4");
                    return;
                }
            }

        }

        if (catLenght == 4) // REHACER. HAY QUE LOOPEAR A PARTIR DE 4 PALABRAS. NO DEBE COMPORTARSE COMO LOS DE ARRIBA
        {
            foreach(WordUIBehaviour slot in slots)
            {
            if (slot.currentSlot == 4 ) slot.ToggleVisibility(false);

            if (slot.currentSlot == 0 && slot == slots[0]) 

            if (slot.currentSlot == 3 && slot == slots[4] && dpadValue == 1)
                {
                    Debug.Log("Tried to go DOWN from slot 4");
                    return;
                }

            }
        }

        foreach(WordUIBehaviour slot in slots)
            {
                // slot.SetExtremesWords();
                slot.RotateSlot(dpadValue);
                Debug.Log("Navigated Word");
                if (slot.currentSlot == 3)
                {
                    melodyUI.ChangeMelodyDisplayed(slot.word.melody);
                    melodyUI.SetYValues();
                }
            }
    
    
    }

    public void NavigateCategory(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        List<WordCategory> available = GetAvailableCategories();
        if (available.Count < 2) return;

        int current = available.IndexOf(currentCategory);
        if (current < 0) current = 0;

        int next = ((current + dpadValue) % available.Count + available.Count) % available.Count;
        currentCategory = available[next];

        Debug.Log(currentCategory);
        UpdateCategoryText();
        GetCategoryLenght();
        AdaptSlotNumber();
        RefreshSlotsForCurrentCategory();
    }

    private List<WordCategory> GetAvailableCategories()
    {
        if (debugUseTotalWordList)
            return System.Enum.GetValues(typeof(WordCategory)).Cast<WordCategory>().ToList();

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

    public void AdaptSlotNumber()
    {
        GetCategoryLenght();
        Debug.Log("Category Lenght: " + catLenght);

        switch (catLenght)
        {
            case 1:
            foreach(WordUIBehaviour slot in slots)
            {
                if (slot.currentSlot == 2) slot.ToggleVisibility(true);
                else slot.ToggleVisibility(false);
            }
            Debug.Log("Only Showing Slot 2");

            break;

            case 2:
            foreach(WordUIBehaviour slot in slots)
            {
                if (slot.currentSlot == 2 || slot.currentSlot == 3) slot.ToggleVisibility(true);
                else slot.ToggleVisibility(false);
            }

            Debug.Log("Showing Slot 2 and 3");

            break;


            default:
            case >3:
            foreach(WordUIBehaviour slot in slots)
            {
                slot.ToggleVisibility(true);
            }
            break;


        }
    }

    public void GetCategoryLenght()
    {
        catLenght = debugUseTotalWordList
            ? totalWordList.GetCategoryLength(currentCategory)
            : (learnedWordList != null ? learnedWordList.GetCategoryLength(currentCategory) : 0);
    }

    private IReadOnlyList<Word> GetActiveWordsByCategory(WordCategory category)
    {
        if (debugUseTotalWordList)
        {
            if (totalWordList == null) return new List<Word>();
            return totalWordList.GetWordsByCategory(category);
        }

        if (learnedWordList == null) return new List<Word>();
        return learnedWordList.GetWordsByCategory(category);
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
