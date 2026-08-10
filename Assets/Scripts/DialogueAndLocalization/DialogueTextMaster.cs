using System.Collections;
using System.Collections.Generic;
using Febucci.TextAnimatorCore;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class DialogueTextMaster : MonoBehaviour
{

    public TextMeshProUGUI tmp;
    public LocalizeStringEvent locString;
    public TypewriterComponent typewriter;
    public TextAnimator_TMP tAnimator;
    public DialogueTrigger trigger;
    public string tableName = "NPCS";

    public void RestartText()
    {   
        tAnimator.SetText(tmp.text);          // re-aplica el texto al TextAnimator
        typewriter.StartShowingText(true);    // true = empezar desde el principio
    }

    public void AssignNewDialogue(params string[] candidateIds)
    {
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

    // Si ninguna candidata existió, fallback del NPC (deriva el nombre del primer id)
    if (text == null && candidateIds.Length > 0)
    {
        string npc = candidateIds[0].Split('_')[1];
        text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, $"npc_{npc}_fallback");
    }

    // Red de seguridad: si seguimos sin texto, no revientes
    if (string.IsNullOrEmpty(text))
    {
        Debug.LogWarning($"No se encontró ninguna key ni fallback. Candidatas: {string.Join(", ", candidateIds)} | Tabla: '{tableName}'");
        text = "...";
    }

    if (tAnimator == null) { Debug.LogError("tAnimator no asignado en DialogueTextMaster"); return; }

    trigger.StartDialogue(text);
    Debug.Log($"Diálogo usado: {usedId ?? "fallback"}");
    }


}
