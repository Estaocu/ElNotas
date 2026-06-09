using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class DialogueTextMaster : MonoBehaviour
{
    public TextMeshProUGUI tmp;
    public LocalizeStringEvent locString;
    public TypewriterComponent typewriter;
    public TextAnimator_TMP tAnimator;
    public DialogueTrigger trigger;
    [SerializeField] private string tableName = "NPCS";

    public void RestartText()
    {
        tAnimator.SetText(tmp.text);
        typewriter.StartShowingText(true);
    }

    public void AssignNewDialogue(params string[] candidateIds)
    {
        // --- DIAGNÓSTICO ---
        var locale = LocalizationSettings.SelectedLocale;
        Debug.Log($"[LocDebug] Locale activo: {(locale != null ? locale.Identifier.Code : "NULL")}");
        Debug.Log($"[LocDebug] tableName en el script: '{tableName}'");
        Debug.Log($"[LocDebug] Candidatas: {string.Join(" | ", candidateIds)}");

        var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        tableOp.WaitForCompletion();
        var table = tableOp.Result;

        if (table == null)
        {
            Debug.LogError($"[LocDebug] No se pudo cargar la StringTable '{tableName}' para el locale activo.");
        }
        else
        {
            Debug.Log($"[LocDebug] Tabla cargada OK: {table.TableCollectionName} (entries: {table.Count})");

            foreach (var sharedEntry in table.SharedData.Entries)
            {
                Debug.Log($"[LocDebug] Key real en tabla: '{sharedEntry.Key}' (length={sharedEntry.Key.Length})");
            }

            string targetKey = "npc_richard_fallback";
            var entry = table.GetEntry(targetKey);
            Debug.Log($"[LocDebug] Buscando '{targetKey}' (length={targetKey.Length}) -> existe?: {entry != null} | Valor: '{entry?.LocalizedValue}'");
        }
        // --- FIN DIAGNÓSTICO ---

        string text = null;
        string usedId = null;

        foreach (string id in candidateIds)
        {
            string result = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, id);
            if (!string.IsNullOrEmpty(result) && !result.StartsWith("No translation"))
            {
                text = result;
                usedId = id;
                break;
            }
        }

        if (text == null && candidateIds.Length > 0)
        {
            string npc = candidateIds[0].Split('_')[1];
            text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, $"npc_{npc}_fallback");

            if (!string.IsNullOrEmpty(text) && !text.StartsWith("No translation"))
                usedId = $"npc_{npc}_fallback";
            else
                text = null;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"No se encontró ninguna key ni fallback. Candidatas: {string.Join(", ", candidateIds)} | Tabla: '{tableName}'");
            text = "...";
        }

        if (tAnimator == null) { Debug.LogError("tAnimator no asignado en DialogueTextMaster"); return; }

        tAnimator.SetText(text);
        typewriter.StartShowingText(true);
        Debug.Log($"Diálogo usado: {usedId ?? "fallback genérico"}");
    }
}
