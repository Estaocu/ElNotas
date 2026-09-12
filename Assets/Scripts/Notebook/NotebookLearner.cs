using UnityEngine;
using CarterGames.Assets.SaveManager;
using Save;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling.Memory.Experimental;


public class NotebookLearner : MonoBehaviour
{
    public static NotebookLearner Instance { get; private set; }


    private NotebookSaveObject saveObject;

    [SerializeField] private List<NotebookEntry> totalEntryList;

    public List<NotebookEntry> learnedEntryList;

    private List<string> idList;

    [Serializable]
    public class CategoryGroup
    {
        public WordCategory category;
        public List<NotebookEntry> entries = new List<NotebookEntry>();

        public CategoryGroup(WordCategory category)
        {
            this.category = category;
            this.entries = new List<NotebookEntry>();
        }

    }

    public List<CategoryGroup> LearnedCategories;



    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (SaveManager.TryGetGlobalSaveObject<NotebookSaveObject>(out saveObject))
        {
            Debug.Log("NotebookLearner | SaveObject global encontrado con éxito.");

            RebuildLearnedEntryList(); 
            Debug.Log("Got Learned Entries");
        }

        else
        {
            Debug.LogWarning("No se pudo encontrar el NotebookSaveObject global. Asegúrate de que está creado y registrado en los ajustes del Save Manager.", this);
        }

          
    }

    public void AddEntry(InputAction.CallbackContext context)
    {

        if (!context.performed) return;

        string testEntry = "ne_w_machine";
        if (saveObject != null && saveObject.LearnedEntriesIds != null && saveObject.LearnedEntriesIds.Value != null)
        {
            if (!saveObject.LearnedEntriesIds.Value.Contains(testEntry))
            {
                saveObject.LearnedEntriesIds.Value.Add(testEntry);

                RebuildLearnedEntryList(); 

                SaveManager.SaveGame();
                Debug.Log("Entry ne_w_machine stored. Game Saved");
            }
        }

        Debug.Log($"Saved ID: {testEntry}");

    // foreach (NotebookEntry entry in totalEntryList)
    // {
    //     Debug.Log( $"Entry: {entry.name} | " + $"Entry Key: {entry.key}");
    // }
    }

    public void CatchDialogueEvent(string eventName, string[] parameters)
    {
        if (!string.Equals(eventName,"LearnEntry",StringComparison.OrdinalIgnoreCase)) return; // Si el event recibido no es de aprender entries me piro
        if (parameters == null || parameters.Length == 0) return; //Si no hay parameters me piro también para prevenir errores y aprender entries vacias

        AddStringToSave(parameters[0]);




    }

    private void AddStringToSave(string id)
    {
        if (saveObject != null && saveObject.LearnedEntriesIds != null && saveObject.LearnedEntriesIds.Value != null)
        {
            if (!saveObject.LearnedEntriesIds.Value.Contains(id))
            {
                saveObject.LearnedEntriesIds.Value.Add(id);

                RebuildLearnedEntryList();

                SaveManager.SaveGame();
                Debug.Log($"Entry {id} stored. Game Saved");
            }
        }
        
    }

    private void RebuildLearnedEntryList()
    {
        GetSavedIds();

        foreach (NotebookEntry entry in totalEntryList)
        {
            if (entry == null || entry.id == null || entry.id.IsEmpty)
                continue;

            Debug.Log(
                $"NotebookEntry: {entry.name} | " +
                $"Key: {entry.key} | " +
                $"Saved: {idList.Contains(entry.key)}"
            );
        }

        learnedEntryList = totalEntryList
            .Where(e => e?.id != null &&
                        !e.id.IsEmpty &&
                        idList.Contains(e.key))
            .ToList();

        RefreshCategories();
    }

    public void GetSavedIds()
    {
        idList = saveObject.LearnedEntriesIds.Value;
    }

    public void RefreshCategories()
    {
        LearnedCategories.Clear();
        var actualCategories = learnedEntryList.Where
        (entry => entry != null && entry.type == EntryType.Word && entry.category.HasValue)
        .Select(entry => entry.category.Value).Distinct().OrderBy(category => category).ToList();    

        foreach (WordCategory cat in actualCategories)
        {
            CategoryGroup group = new CategoryGroup(cat)
            {
                entries = learnedEntryList.Where
                (entry => entry != null && entry.type == EntryType.Word && entry.category.HasValue && entry.category.Value == cat)
                .OrderBy(entry => entry.key != null ? entry.key : string.Empty, StringComparer.CurrentCultureIgnoreCase).ToList()
            };

            LearnedCategories.Add(group);
            
            
        }
        Debug.Log($"Categories: {LearnedCategories.Count}");
    }


}
