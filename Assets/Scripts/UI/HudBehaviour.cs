using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudBehaviour : MonoBehaviour
{
    private MetronomeManager metronome;
    private MeterUIManager meter; 
    private NotesUIManager notes;


    void Awake()
    {
        metronome = GetComponentInChildren<MetronomeManager>();
        meter = GetComponentInChildren<MeterUIManager>();
        notes = GetComponentInChildren<NotesUIManager>();
    }

    public void HideHud()
    {
        metronome.ToggleVisibility(false);
        meter.gameObject.SetActive(false);
        notes.CleanAllNotes();
    }

    public void ShowHud()
    {
        metronome.ToggleVisibility(true);
        meter.gameObject.SetActive(true);
    }
}
