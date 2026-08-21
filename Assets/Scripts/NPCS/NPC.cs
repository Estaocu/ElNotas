using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using CarterGames.Assets.SaveManager; 
using Save;
using NaughtyAttributes;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using System.Collections.Generic;

public enum DialogueMode { Intro, Monologue, Answer, Question, Goodbye };

public class NPC : MonoBehaviour
{
    public string npcName;
    
    public DialogueMode currentMode;
    private DialogueMode ogMode;
    [ShowIf("currentMode", DialogueMode.Intro)]
    [Dropdown(nameof(afterIntroOptions))]
    public DialogueMode afterIntro = DialogueMode.Answer;

    private DialogueMode[] afterIntroOptions => new DialogueMode[]
    {
        DialogueMode.Answer,
        DialogueMode.Question
    };

    [HorizontalLine(color: EColor.Blue)]
    [SerializeField] private DialogueManager manager;
    [SerializeField] private DialogueTextMaster master;
    [SerializeField] private ConversationManager conversationUI;
    [SerializeField] private SingleNotesListener listener;

    [HideInInspector] public LocalizedString myDialogue;
    [HideInInspector] public int timesSpoken;

    private NpcsSaveObject npcSaveObject;
    private bool wantsNotes;

    private void Start()
    {
        ogMode = currentMode;

        wantsNotes = true;

        List<notesEnum[]> melodies = new List<notesEnum[]>();
        notesEnum[] greeting = new notesEnum[]{notesEnum.Note4, notesEnum.Note3};
        melodies.Add(greeting);

        listener.SetDesiredMelodies(melodies);

        if (SaveManager.TryGetGlobalSaveObject<NpcsSaveObject>(out npcSaveObject))
        {
            if (npcSaveObject.ContainsKey(npcName))
            {
                timesSpoken = npcSaveObject.GetTimesSpoken(npcName);
                Debug.Log($"[SaveSystem] Progress loaded for {npcName}: spoken {timesSpoken} times.");
            }
            else
            {
                npcSaveObject.SetTimesSpoken(npcName, 0);
                timesSpoken = 0;
            }
        }
        else
        {
            Debug.LogWarning("[SaveSystem] NpcsSaveObject not found in Save Manager.");
        }
    }

    public void BeTheOne()
    {
        manager.canInteract = currentMode != DialogueMode.Answer && currentMode != DialogueMode.Question;
        Debug.Log($"Can interact: {manager.canInteract}");
        
        if (manager != null && manager.canInteract)
        {
            manager.SetNewNPC(this);
        }

        if (conversationUI != null)
        {
            conversationUI.SetNewNPC(this);
        }
    }

    public void FindDialogue()
    {
        switch (currentMode)
        {
            case DialogueMode.Intro:
                if (!wantsNotes) return;
                string intro = $"npc_{npcName.ToLower()}_intro";
                master.AssignNewDialogue(intro);
                Debug.Log($"<color=#C5FF10>Assigned dialogue {intro}</color>");
                break;

            case DialogueMode.Monologue:
                master.trigger.ToggleInConv(false);
                string monologue = $"npc_{npcName.ToLower()}_monologue{timesSpoken}";
                master.AssignNewDialogue(monologue);
                Debug.Log($"<color=#C5FF10>Assigned dialogue {monologue}</color>");
                IncreaseTimesSpoken();
                break;


            case DialogueMode.Goodbye:

                string goodbye =  $"npc_{npcName.ToLower()}_goodbye";
                Debug.Log($"<color=#C5FF10>Assigned dialogue {goodbye}</color>");
                master.AssignNewDialogue(goodbye);

                break;

            default:
                Debug.LogWarning($"<color=#FF4310>NPC {npcName} attempted to find a dialogue outside Intro or Monologue modes.</color>");
                break;
        }
    }

    // Public method invoked exclusively by DialogueManager for the active NPC
    public void HandleDialogueEvent(string eventName, string[] parameters)
    {
        if (eventName.Equals("setMode", System.StringComparison.OrdinalIgnoreCase))
        {
            if (parameters != null && parameters.Length > 0 && System.Enum.TryParse(parameters[0], out DialogueMode newMode))
            {
                ChangeDialogueMode(newMode);
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
    }

    public void ResetMode()
    {
        currentMode = ogMode;
    }
}