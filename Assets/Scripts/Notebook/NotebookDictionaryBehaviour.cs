using System.Collections;
using System.Collections.Generic;
using CarterGames.Assets.SaveManager.Slots;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using NaughtyAttributes;
using CarterGames.Assets.SaveManager;
using UnityEngine.Localization.Settings;

public enum ChapterType {Dictionary, Melodies}

public class NotebookDictionaryBehaviour : MonoBehaviour
{


    public int currentPage = 0;
    public int maxPage;
    public WordCategory currentCategory;
    private IReadOnlyList<NotebookEntry> currentWordList;
    [SerializeField] private NotebookDictionarySlot[] slots;
    [SerializeField] private TextMeshProUGUI[] pageNums; 
    [SerializeField] private TextMeshProUGUI chapterTxt;
    [SerializeField] private TextMeshProUGUI categoryTxt;

    private string tableName = "Notebook";

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
        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out var notebookSave))
        {
            List<string> learnedEntries = notebookSave.LearnedEntriesIds.Value;
        }





        for (int i = 0; i < chapters.Length; i++)
    {
        string id = $"notebookChapter_{i}";
        chapters[i].chapterName = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, id);
    }

        

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
    }


    private void UpdatePageNumDisplay()
    {
        int pageA = 2 * currentPage -1;
        int pageB = pageA+1;
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

    public void CalculateNotebook()
    {
        /// 1. Get all learned entries
        /// 2. Get all different Chapters
        /// 3. Get all different Categories
        /// 4. Handle Chapter 0: Send entries to slots based on learned order and separate with categories
        /// 5. Handle Chapter 1: Send entries to slots based on learned order
        /// 6. Get chapter 0 page amount
        /// 7. Get chapter 1 page amount
    }

    
    
}
