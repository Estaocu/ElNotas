using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using CarterGames.Assets.SaveManager; 
using Save;
using Unity.VisualScripting;

public enum DialogueMode { Intro, Monologue, Answer, Question };

public class NPC : MonoBehaviour
{
    public string npcName;
    public int timesSpoken;

    public DialogueMode currentMode;
    
    [SerializeField] private DialogueManager manager;
    [SerializeField] private DialogueTextMaster master;
    [SerializeField] private DialogueTrigger trigger;

    [HideInInspector] 
    public LocalizedString myDialogue;

    private NpcsSaveObject npcSaveObject;

    private void Start()
    {
        if (SaveManager.TryGetGlobalSaveObject<NpcsSaveObject>(out npcSaveObject))
        {
            if (npcSaveObject.ContainsKey(npcName))
            {
                timesSpoken = npcSaveObject.GetTimesSpoken(npcName);
                Debug.Log($"[SaveSystem] Cargado progreso de {npcName}: se ha hablado con él {timesSpoken} veces.");
            }
            else
            {
                npcSaveObject.SetTimesSpoken(npcName, 0);
                timesSpoken = 0;
            }
        }
        else
        {
            Debug.LogWarning("[SaveSystem] No se encontró el NpcsSaveObject en el Save Manager.");
        }
    }

    public void BeTheOne()
{
    if (currentMode == DialogueMode.Answer || currentMode == DialogueMode.Question) manager.canInteract = false;
    else manager.canInteract = true;
    Debug.Log($"can interact: {manager.canInteract}");
    if (manager != null && manager.canInteract)
    {
        manager.SetNewNPC(this);
    }
}

    public void FindDialogue()
    {
        switch (currentMode)
        {
            case DialogueMode.Intro:
                string intro = $"npc_{npcName.ToLower()}_intro";

                // Enqueues tag <?setMode=Answer> to be added at the end of the last page
                if (trigger != null)
                {
                    trigger.AddEndTagToNextDialogue("setMode", DialogueMode.Answer.ToString());
                }

                master.AssignNewDialogue(intro);
                Debug.Log($"<color=#C5FF10>Assigned dialogue {intro}</color>");
                break;

            case DialogueMode.Monologue:
                string monologue = $"npc_{npcName.ToLower()}_monologue{timesSpoken}";
                master.AssignNewDialogue(monologue);
                Debug.Log($"<color=#C5FF10>Assigned dialogue {monologue}</color>");
                IncreaseTimesSpoken();
                break;

            default:
                Debug.LogWarning($"<color=#FF4310>NPC {npcName} ha intentado buscar un diálogo NO estando en Intro ni Monologue.</color>");
                break;
        }
    }

    // Public method invoked exclusively by DialogueManager for the active NPC
    public void HandleDialogueEvent(string eventName, string[] parameters)
    {
        if (eventName.Equals("setMode", System.StringComparison.OrdinalIgnoreCase))
        {
            if (parameters != null && parameters.Length > 0)
            {
                if (System.Enum.TryParse(parameters[0], out DialogueMode newMode))
                {
                    Debug.Log($"<color=#34ebcf>[{npcName}] => Mode.Answer</color>");
                    //currentMode = newMode;
                    //Debug.Log($"[NPC] {npcName} switched mode to: <color=#C5FF10>{currentMode}</color>");
                }
            }
        }
    }

    public int GetMaxMonologues(string tableName = "NPCS")
    {
        var table = LocalizationSettings.StringDatabase.GetTable(tableName);
        if (table == null) return 0;

        string prefix = $"npc_{npcName.ToLower()}_monologue";

        return table.Values
            .Select(entry => entry.Key)
            .Where(key => key.StartsWith(prefix))
            .Select(key => int.TryParse(key.Substring(prefix.Length), out int num) ? num : -1)
            .DefaultIfEmpty(-1)
            .Max();
    }

    public void IncreaseTimesSpoken()
    {
        timesSpoken = Mathf.Min(timesSpoken + 1, GetMaxMonologues());

        if (npcSaveObject != null)
        {
            npcSaveObject.SetTimesSpoken(npcName, timesSpoken);
            SaveManager.SaveGame();
        }
    }

    public void ChangeDialogueMode(DialogueMode newMode)
    {
        currentMode = newMode;
        Debug.Log($"[NPC] {npcName} switched mode to: <color=#C5FF10>{currentMode}</color>");

        //to do: en caso de cambiar a answer:
        //Visual operations to bubble
        //Enable Q&A UI
        //Change player controls to dialogue

    }
}