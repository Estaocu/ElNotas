using UnityEngine;
using UnityEngine.Localization;

public enum DialogueMode {Intro, Monologue, Answer, Question};
public class NPC : MonoBehaviour
{
    public string npcName;

    
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
            string monologue  = $"npc_{npcName.ToLower()}_monologue";
            master.AssignNewDialogue(monologue);
            Debug.Log($"<color=#C5FF10>Assigned dialogue {monologue}</color>");
            break;

            default:
            Debug.LogWarning($"<color=#FF4310>NPC {npcName} no tiene Start Mode definido.</color>");
            break;
            
        }
    }






}
