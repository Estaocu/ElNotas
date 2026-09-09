using System;
using System.Collections.Generic;
using System.Linq;
using CMF;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class ConversationManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;

    public WordUIBehaviour[] slots;

    [SerializeField] private NotebookSaveObject notebookSaveObject;
    [SerializeField] private List<NotebookEntry> allNotebookEntries;

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
        if (notebookSaveObject == null || allNotebookEntries == null)
        {
            return new List<WordCategory>();
        }

        // Obtener las entradas aprendidas a partir de los IDs guardados
        List<string> learnedIds = notebookSaveObject.LearnedEntriesIds.Value;

        return allNotebookEntries
            .Where(entry => learnedIds.Contains(entry.id.TableEntryReference.Key) || learnedIds.Contains(entry.name))
            .Where(entry => entry.category.HasValue)
            .Select(entry => entry.category.Value)
            .Distinct()
            .OrderBy(category => (int)category)
            .ToList();
    }

    

    public void UpdateCategoryLength()
    {
        currentWordList = GetActiveWordsByCategory(currentCategory);

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

        List<WordCategory> available =
            GetAvailableCategories();

        lastIndex = new int[available.Count];

        if (debugNum != null)
        {
            debugNum.SetText(centerWordIndex.ToString());
        }

        UpdateCategoryText();

        //AdaptSlotNumber();

        UpdateCategoryLength();
        AssignWordsToSlots();

        Debug.Log("CONVERSATION (re)/enabled");

        IndexToWord();

        if (npcText != null)
        {
            npcText.transform.root.gameObject.SetActive(true);
        }

        if (debugWord != null &&
            debugWord.entry != null &&
            melodyUI != null)
        {
            melodyUI.ChangeMelodyDisplayed(
                debugWord.entry.melody
            );

            melodyUI.SetYValues();
        }

        if (currentNpc != null &&
            npcText != null)
        {
            string txt =
                $"npc_{currentNpc.npcName.ToLower()}_answer0";

            npcText.AssignNewDialogue(txt);
        }
    }

    private void ClampCategoryToAvailable()
    {
        List<WordCategory> available =
            GetAvailableCategories();

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

    public void NavigateWords(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        float axisValue =
            context.ReadValue<float>();

        int dpadValue =
            (int)Mathf.Sign(axisValue);

        if (catLength <= 1)
        {
            //PlayErrorSFX();
            return;
        }

        if (catLength < 4)
        {
            int targetIndex =
                centerWordIndex - dpadValue;

            // Strict limits for 2 or 3 entries.
            if (targetIndex < 0 ||
                targetIndex >= catLength)
            {
                //PlayErrorSFX();
                return;
            }

            centerWordIndex = targetIndex;
        }
        else
        {
            // Circular navigation for 4 or more entries.
            centerWordIndex =
                (centerWordIndex - dpadValue + catLength)
                % catLength;
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

        //UpdateSlotsVisuals(false);
    }

    public void NavigateCategory(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        float axisValue =
            context.ReadValue<float>();

        int dpadValue =
            (int)Mathf.Sign(axisValue);

        List<WordCategory> available =
            GetAvailableCategories();

        if (available.Count < 2)
            return;

        int current =
            available.IndexOf(currentCategory);

        if (current < 0)
            current = 0;

        // Save the last entry hovered in the current category.
        if (lastIndex != null &&
            current < lastIndex.Length)
        {
            lastIndex[current] =
                centerWordIndex;

            Debug.Log(
                $"Category [{current}|{currentCategory}] " +
                $"Last index: {lastIndex[current]}"
            );
        }

        int next =
            ((current + dpadValue) % available.Count
             + available.Count)
            % available.Count;

        currentCategory =
            available[next];

        UpdateCategoryText();
        UpdateCategoryLength();

        //AdaptSlotNumber();

        AssignWordsToSlots();

        Debug.Log(
            $"Category is now {currentCategory} | " +
            $"Length: {catLength}"
        );

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

        if (debugWord != null &&
            debugWord.entry != null &&
            melodyUI != null)
        {
            melodyUI.ChangeMelodyDisplayed(
                debugWord.entry.melody
            );

            melodyUI.SetYValues();
        }

        //UpdateSlotsVisuals(false);
    }

    private void UpdateCategoryText()
    {
        if (categoryText == null)
            return;

        string id =
            $"category_{currentCategory}";

        string localized =
            LocalizationSettings.StringDatabase
                .GetLocalizedString(
                    categoryTableName,
                    id
                );

        if (string.IsNullOrEmpty(localized) ||
            localized.StartsWith("No translation"))
        {
            Debug.LogWarning(
                $"No localized string for '{id}' " +
                $"in table '{categoryTableName}'"
            );

            localized =
                currentCategory.ToString();
        }

        categoryText.SetText(localized);
    }

    public void SelectWord(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        for (int i = 0;
             i < bubbleWords.Length;
             i++)
        {
            if (bubbleWords[i].full)
                continue;

            bubbleWords[i].PlaceWord(
                debugWord.entry
            );

            // If it was the last empty bubble,
            // the sentence is complete.
            if (i == bubbleWords.Length - 1)
            {
                FindAnswer();
            }

            return;
        }

        // foreach (WordUIBehaviour slot in slots)
        // {
        //     if (slot.currentSlot != 2)
        //         continue;
        //
        //     for (int i = 0;
        //          i < bubbleWords.Length;
        //          i++)
        //     {
        //         if (bubbleWords[i].full)
        //             continue;
        //
        //         bubbleWords[i].PlaceWord(slot.entry);
        //
        //         // If it was the last empty bubble,
        //         // the sentence is complete.
        //         if (i == bubbleWords.Length - 1)
        //             FindAnswer();
        //
        //         return;
        //     }
        // }
    }

    public void EraseWord(
        InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        for (int i = bubbleWords.Length - 1;
             i >= 0;
             i--)
        {
            if (!bubbleWords[i].full)
                continue;

            bubbleWords[i].EraseWord();
            return;
        }
    }

    public void FindAnswer()
    {
        npcText.trigger.ToggleInConv(true);

        string a =
            bubbleWords[0].entry.id
                .ToString()
                .ToLower();

        string b =
            bubbleWords[1].entry.id
                .ToString()
                .ToLower();

        string npc =
            currentNpc.npcName.ToLower();

        // Alphabetical sorting.
        if (string.CompareOrdinal(a, b) > 0)
        {
            (a, b) = (b, a);
        }

        string primaryId =
            $"npc_{npc}_{a}{b}";

        string swappedId =
            $"npc_{npc}_{b}{a}";

        Debug.Log(
            "Finding dialogue: " +
            primaryId
        );

        npcText.AssignNewDialogue(
            primaryId,
            swappedId
        );

        ActionMapsManager.SetActiveMaps(
            DefaultActionMap.Text
        );
    }

    private IReadOnlyList<NotebookEntry> GetActiveWordsByCategory(WordCategory category)
    {
        if (notebookSaveObject == null || allNotebookEntries == null)
        {
            return new List<NotebookEntry>();
        }

        // 1. Obtener la lista de IDs guardados desde el SaveObject
        List<string> learnedIds = notebookSaveObject.LearnedEntriesIds.Value;

        // 2. Filtrar el catálogo global de entradas haciendo coincidir el ID/Key y la categoría
        return allNotebookEntries
            .Where(entry => entry.category.HasValue && entry.category.Value == category)
            .Where(entry => 
                // Valida si la clave guardada coincide con la Key de localización o el nombre del asset
                learnedIds.Contains(entry.id.TableEntryReference.Key) || 
                learnedIds.Contains(entry.name)
            )
            .ToList();
    }


    private void AssignWordsToSlots()
    {
        currentWordList = GetActiveWordsByCategory(currentCategory);

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
        if (currentWordList == null ||
            currentWordList.Count == 0)
        {
            Debug.LogWarning(
                "IndexToWord skipped: " +
                "currentWordList is null or empty."
            );

            return;
        }

        // Ensure centerWordIndex stays safely
        // within currentWordList bounds.
        int index =
            ((centerWordIndex %
              currentWordList.Count)
             + currentWordList.Count)
            % currentWordList.Count;

        NotebookEntry selectedEntry =
            currentWordList[index];

        if (selectedEntry == null)
        {
            Debug.LogError(
                $"NotebookEntry at index {index} " +
                $"in category {currentCategory} is null!"
            );

            return;
        }

        debugWord.DisplayNewWord(
            selectedEntry
        );
    }

    public void ReturnConversation()
    {
        foreach (
            PlayerBubbleWordBehaviour bubble
            in bubbleWords)
        {
            bubble.EraseWord();
        }

        ActionMapsManager.SetActiveMaps(
            DefaultActionMap.Conversation
        );

        string txt =
            $"npc_{currentNpc.npcName.ToLower()}_answer1";

        npcText.AssignNewDialogue(txt);
    }
}