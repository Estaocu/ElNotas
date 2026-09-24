using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NotebookDatabase",
    menuName = "ScriptableObjects/Notebook Database",
    order = 1
)]
public class NotebookDatabase : ScriptableObject
{
    [Tooltip("Arrastra aquí todos los ScriptableObjects de tipo NotebookEntry que existan en tu juego.")]
    public List<NotebookEntry> allEntries = new List<NotebookEntry>();

    // Método rápido para buscar la referencia física a partir de su ID de localización string
    public NotebookEntry GetEntryByID(string stringID)
    {
        if (string.IsNullOrEmpty(stringID)) return null;
        return allEntries.Find(entry => entry.id.TableEntryReference.Key == stringID);
    }
}