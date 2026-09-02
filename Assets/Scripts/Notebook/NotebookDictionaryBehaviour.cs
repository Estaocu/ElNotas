using System.Collections;
using System.Collections.Generic;
using CarterGames.Assets.SaveManager.Slots;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using NaughtyAttributes;
using CarterGames.Assets.SaveManager;

public enum ChapterType {Dictionary, Melodies}

public class NotebookDictionaryBehaviour : MonoBehaviour
{


    public int currentPage = 0;
    public int maxPage;
    [SerializeField] private LearnedWords learnedWordList;
    public WordCategory currentCategory;
    private IReadOnlyList<Word> currentWordList;
    [SerializeField] private NotebookDictionarySlot[] slots;
    [SerializeField] private TextMeshProUGUI[] pageNums; 
    [SerializeField] private TextMeshProUGUI chapterTxt;
    [SerializeField] private TextMeshProUGUI categoryTxt;

    public NotebookChapter[] chapters;
    public NotebookChapter currentChapter;

    [System.Serializable]
    public class NotebookChapter
    {
        public string chapterName;
        public LocalizedString chapterLocString;
        public int pageAmount;
        public NotebookEntry[] entries;
        public bool isUnlocked;
    }
    

    private void Start()
    {
        UpdateTotalPageCount();
        UpdateCurrentChapter();
        UpdateHeader();
        UpdatePageNumDisplay();
    }




    public void FlipPage(InputAction.CallbackContext context)
    {
        int ogPage = currentPage;

        if (!context.performed) return;

        //Debug.Log("Tap");
        
        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        if (currentPage >= maxPage && dpadValue > 0) return;
        if (currentPage <= 1 && dpadValue < 0) return;

        currentPage = Mathf.Clamp (currentPage + dpadValue, 1, maxPage);
        Debug.Log($"[Flip] currentPage {currentPage}");

        //UpdatePageNumDisplay();

        bool noChange = ogPage == currentPage;

        if (noChange) return;

        UpdateCurrentChapter();
        UpdatePageNumDisplay();

        foreach(NotebookDictionarySlot slot in slots)
        {
            slot.RecalculateDisplay();
        }
    }

    public void ChangeChapter(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);


        if (currentChapter == chapters[0])
        {
            if (dpadValue < 0) return;

            GoToExactPage(chapters[0].pageAmount + 1);
        }


        if (currentChapter == chapters[1])
        {
            if (dpadValue > 0) return;

            GoToExactPage(1);
        }


        UpdateCurrentChapter();
        Debug.Log($"Chapter changed to {currentChapter.chapterName} | Current page: {currentPage}");
        



    }

    public void GoToExactPage(int desiredPage)
    {
        currentPage = desiredPage;
        UpdatePageNumDisplay();
        Debug.Log($"[PageTP] Went to currentPage {currentPage}");

        foreach(NotebookDictionarySlot slot in slots)
        {
            slot.RecalculateDisplay();
        }
    }


    private void UpdatePageNumDisplay()
    {
        int pageA = currentPage;
        int pageB = currentPage+1;
        pageNums[0].SetText(pageA.ToString());
        pageNums[1].SetText(pageB.ToString());
    }

    public void UpdateChapterPageAmount(ChapterType chapter, int pagesN)
    {

        if (chapter == ChapterType.Dictionary)
        {
            chapters[0].pageAmount = pagesN;
        }

        if (chapter == ChapterType.Melodies)
        {
            chapters[1].pageAmount = pagesN;
        }

        UpdateTotalPageCount();

    }

    public void DebugAddPages(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        float axisValue = context.ReadValue<float>();
        int dpadValue = (int)Mathf.Sign(axisValue);

        if (dpadValue < 0)
        {
            chapters[0].pageAmount++;
        }

        if (dpadValue > 0)
        {
            chapters[1].pageAmount++;
        }

        UpdateTotalPageCount();

        
    }

    public void UpdateTotalPageCount()
    {
        maxPage = chapters[0].pageAmount + chapters[1].pageAmount;
        Debug.Log($"Total pages: {maxPage} | Dictionary pages: {chapters[0].pageAmount} | Melodies pages: {chapters[1].pageAmount}");
    }

    public void UpdateCurrentChapter()
    {
        if (currentPage > chapters[0].pageAmount)
        {
            currentChapter = chapters[1];

        }

        if (currentPage <= chapters[0].pageAmount)
        {
            currentChapter = chapters[0];
        }

        UpdateHeader();
        Debug.Log($"Chapter changed to {currentChapter.chapterName} | Current page: {currentPage}");
    }

    public void UpdateHeader()
    {
        chapterTxt.SetText(currentChapter.chapterName);
        //categoryTxt.SetText(currentCategory.displayName.ToString());
    }

    
    
}
