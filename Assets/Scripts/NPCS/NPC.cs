using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public enum DialogueMode {Intro, Monologue, Answer, Question};
public class NPC : MonoBehaviour
{
    public string npcName;
    public int timesSpoken;

    
    public DialogueMode startMode;
    
    [SerializeField] private DialogueManager manager;
    [SerializeField] private DialogueTextMaster master;

    [HideInInspector] 
    public LocalizedString myDialogue;

    public void BeTheOne() => manager.SetNewNPC(this);

    public void FindDialogue()
    {
        switch (startMode)
        {
            case DialogueMode.Intro:
            string intro = $"npc_{npcName.ToLower()}_intro";
            master.AssignNewDialogue(intro);
            Debug.Log($"<color=#C5FF10>Assigned dialogue {intro}</color>");
            break;

            case DialogueMode.Monologue:
            string monologue  = $"npc_{npcName.ToLower()}_monologue{timesSpoken}";
            master.AssignNewDialogue(monologue);
            Debug.Log($"<color=#C5FF10>Assigned dialogue {monologue}</color>");
            IncreaseTimesSpoken();
            break;

            default:
            Debug.LogWarning($"<color=#FF4310>NPC {npcName} no tiene Start Mode definido.</color>");
            break;
            
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
    }






}
