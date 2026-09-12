using System.Collections.Generic;
using CarterGames.Assets.SaveManager;
using Save;
using UnityEngine;

public class NotebookSaveObject : SaveObject
{
    [SerializeField]
    private SaveValue<List<string>> learnedEntriesIds = new SaveValue<List<string>>( "learned_entries_ids",new List<string>());

    public SaveValue<List<string>> LearnedEntriesIds => learnedEntriesIds;
}