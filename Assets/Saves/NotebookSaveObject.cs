using System.Collections.Generic;
using CarterGames.Assets.SaveManager;
using UnityEngine;

namespace Save
{
    public class NotebookSaveObject : SaveObject
    {

        [SerializeField]
        private SaveValue<List<string>> notebookEntries = new SaveValue<List<string>>("notebook_entries", new List<string>());

        [SerializeField] 
        private SaveValue<List<string>> learnedWordIds = new SaveValue<List<string>>("learned_word_ids", new List<string>());

        public SaveValue<List<string>> LearnedWordIDs => learnedWordIds;
    }
}