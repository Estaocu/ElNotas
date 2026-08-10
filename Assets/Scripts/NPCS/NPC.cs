using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

using CarterGames.Assets.SaveManager; 
using Save; 

public enum DialogueMode { Intro, Monologue, Answer, Question };

public class NPC : MonoBehaviour
{
    public string npcName;
    public int timesSpoken;

    public DialogueMode startMode;
    
    [SerializeField] private DialogueManager manager;
    [SerializeField] private DialogueTextMaster master;

    [HideInInspector] 
    public LocalizedString myDialogue;

    private NpcsSaveObject npcSaveObject;

    private void Start()
    {
        // 1. Intentamos obtener el objeto de guardado global al inicio
        if (SaveManager.TryGetGlobalSaveObject<NpcsSaveObject>(out npcSaveObject))
        {
            // 2. Comprobamos si este NPC ya tiene datos registrados utilizando el método helper
            if (npcSaveObject.ContainsKey(npcName))
            {
                timesSpoken = npcSaveObject.GetTimesSpoken(npcName);
                Debug.Log($"[SaveSystem] Cargado progreso de {npcName}: se ha hablado con él {timesSpoken} veces.");
            }
            else
            {
                // Si es la primera vez que interactuamos con él, lo registramos con 0
                npcSaveObject.SetTimesSpoken(npcName, 0);
                timesSpoken = 0;
            }
        }
        else
        {
            Debug.LogWarning("[SaveSystem] No se encontró el NpcsSaveObject en el Save Manager.");
        }
    }

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
                string monologue = $"npc_{npcName.ToLower()}_monologue{timesSpoken}";
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

        // 3. Guardamos el nuevo valor en nuestro SaveObject global
        if (npcSaveObject != null)
        {
            npcSaveObject.SetTimesSpoken(npcName, timesSpoken);

            // Guardamos inmediatamente en el almacenamiento persistente
            SaveManager.SaveGame();
        }
    }
}