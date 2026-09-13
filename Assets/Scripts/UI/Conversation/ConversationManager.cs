using System;
using System.Collections.Generic;
using System.Linq;
using CMF;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using static NotebookLearner;

public class ConversationManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;

    public WordUIBehaviour[] slots;

    public WordCategory currentCategory;

    private IReadOnlyList<NotebookEntry> currentWordList;
    private int catLength;

    public MelodyUI melodyUI;

    private NPC currentNpc;

    [SerializeField] private DialogueTextMaster npcText;

    [SerializeField] private PlayerBubbleWordBehaviour[] bubbleWords =
        new PlayerBubbleWordBehaviour[2];

    [SerializeField] private TextMeshProUGUI categoryText;

    [SerializeField] private string categoryTableName = "WordsNotebook";

    [SerializeField] private TextMeshProUGUI debugNum;

    [SerializeField] private WordUIBehaviour debugWord;

    private int centerWordIndex = 0;
    private int[] lastIndex;

    private List<WordCategory> GetAvailableCategories()
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ConversationManager] NotebookLearner Instance no encontrada.");
            return new List<WordCategory>();
        }

        return Instance.LearnedCategories
            .Where(group => group != null)
            .Select(group => group.category)
            .Distinct()
            .OrderBy(category => (int)category)
            .ToList();
    }

    public void UpdateCategoryLength()
    {
        currentWordList = GetThisCategoryEntries(currentCategory);

        catLength = currentWordList != null ? currentWordList.Count : 0;
    }

    public void SetUIActive(bool desiredState)
    {
        gameObject.SetActive(desiredState);
    }

    private void Start()
    {
        UpdateCategoryLength();
    }

    private void OnEnable()
    {
        ClampCategoryToAvailable();

        List<WordCategory> available = GetAvailableCategories();

        lastIndex = new int[available.Count];

        if (debugNum != null)
        {
            debugNum.SetText(centerWordIndex.ToString());
        }

        UpdateCategoryText();

        UpdateCategoryLength();
        AssignWordsToSlots();

        Debug.Log("CONVERSATION (re)/enabled");

        IndexToWord();

        if (npcText != null)
        {
            npcText.transform.root.gameObject.SetActive(true);
        }

        if (debugWord != null && debugWord.entry != null && melodyUI != null)
        {
            melodyUI.ChangeMelodyDisplayed(debugWord.entry.melody);

            melodyUI.SetYValues();
        }

        if (currentNpc != null && npcText != null)
        {
            string txt = $"npc_{currentNpc.npcName.ToLower()}_answer0";

            npcText.AssignNewDialogue(txt);
        }
    }

    private void ClampCategoryToAvailable()
    {
        List<WordCategory> available = GetAvailableCategories();

        if (available.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!available.Contains(currentCategory))
        {
            currentCategory = available[0];
        }
    }

    public void NavigateWords(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        float axisValue = context.ReadValue<float>();

        int dpadValue = (int)Mathf.Sign(axisValue);

        if (catLength <= 1)
        {
            return;
        }

        if (catLength < 4)
        {
            int targetIndex = centerWordIndex - dpadValue;

            if (targetIndex < 0 || targetIndex >= catLength)
            {
                return;
            }

            centerWordIndex = targetIndex;
        }
        else
        {
            centerWordIndex = (centerWordIndex - dpadValue + catLength) % catLength;
        }

        if (debugNum != null)
        {
            debugNum.SetText(
                centerWordIndex.ToString()
            );
        }

        IndexToWord();

        if (debugWord != null &&
            debugWord.entry != null &&
            melodyUI != null)
        {
            melodyUI.ChangeMelodyDisplayed(
                debugWord.entry.melody
            );

            melodyUI.SetYValues();
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

        if (lastIndex != null && current < lastIndex.Length)
        {
            lastIndex[current] = centerWordIndex;
        }

        int next = ((current + dpadValue) % available.Count + available.Count) % available.Count;

        currentCategory = available[next];

        UpdateCategoryText();
        UpdateCategoryLength();

        AssignWordsToSlots();

        if (lastIndex != null &&
            next < lastIndex.Length)
        {
            centerWordIndex =
                lastIndex[next];
        }
        else
        {
            centerWordIndex = 0;
        }

        if (debugNum != null)
        {
            debugNum.SetText(
                centerWordIndex.ToString()
            );
        }

        IndexToWord();

        if (debugWord != null && debugWord.entry != null && melodyUI != null)
        {
            melodyUI.ChangeMelodyDisplayed(debugWord.entry.melody);

            melodyUI.SetYValues();
        }
    }

    private void UpdateCategoryText()
    {
        if (categoryText == null)
            return;

        string id = $"category_{currentCategory}";

        string localized = LocalizationSettings.StringDatabase.GetLocalizedString(categoryTableName,id);

        if (string.IsNullOrEmpty(localized) || localized.StartsWith("No translation"))
        {
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

            bubbleWords[i].PlaceWord(debugWord.entry);

            if (i == bubbleWords.Length - 1)
            {
                FindAnswer();
            }

            return;
        }
    }

    public void EraseWord(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        for (int i = bubbleWords.Length - 1;i >= 0; i--)
        {
            if (!bubbleWords[i].full) continue;

            bubbleWords[i].EraseWord(); return;
        }
    }

    public void FindAnswer()
    {
        if (npcText != null && npcText.trigger != null)
        {
            npcText.trigger.ToggleInConv(true);
        }

        string a = bubbleWords[0].entry.key.Substring(5);
        string b = bubbleWords[1].entry.key.Substring(5);

        string npc = currentNpc != null ? currentNpc.npcName.ToLower() : string.Empty;

        // Orden alfabético
        if (string.CompareOrdinal(a, b) > 0)
        {
            (a, b) = (b, a);
        }

        string primaryId = $"npc_{npc}_{a}{b}";
        string swappedId = $"npc_{npc}_{b}{a}";

        Debug.Log("Finding dialogue: " + primaryId);

        if (npcText != null)
        {
            npcText.AssignNewDialogue(primaryId,swappedId);
        }

        ActionMapsManager.SetActiveMaps(DefaultActionMap.Text);
    }

    private IReadOnlyList<NotebookEntry> GetThisCategoryEntries(WordCategory category)
    {
        if (Instance == null)   return new List<NotebookEntry>();
        
        return Instance.LearnedCategories
        .FirstOrDefault(x => x.category == category)
        ?.entries ?? new List<NotebookEntry>();
    }

    private void AssignWordsToSlots()
    {
        currentWordList = GetThisCategoryEntries(currentCategory);

        if (currentWordList == null || currentWordList.Count == 0)
        {
            catLength = 0;
            return;
        }

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
            return;
        }

        int index = ((centerWordIndex % currentWordList.Count) + currentWordList.Count) % currentWordList.Count;

        NotebookEntry selectedEntry = currentWordList[index];

        if (selectedEntry == null)
        {
            return;
        }

        debugWord.DisplayNewWord(selectedEntry);
    }

    public void ReturnConversation()
    {
        foreach (PlayerBubbleWordBehaviour bubble in bubbleWords)
        {
            bubble.EraseWord();
        }

        ActionMapsManager.SetActiveMaps(DefaultActionMap.Conversation);

        if (currentNpc != null && npcText != null)
        {
            string txt =
                $"npc_{currentNpc.npcName.ToLower()}_answer1";

            npcText.AssignNewDialogue(txt);
        }
    }
}