using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotebookEntryLearner : MonoBehaviour
{
    public void HandleDialogueEvent(string eventName, string[] parameters)
    {
        if (eventName.Equals("LearnEntry", StringComparison.OrdinalIgnoreCase))
        {
            if (parameters.Length > 0)
            {
                string entryId = parameters[0];
                UnlockJournalEntry(entryId);
            }
        }
    }

    private void UnlockJournalEntry(string entryId)
    {
        Debug.Log($"Journal entry unlocked: {entryId}");
        
    }
}
