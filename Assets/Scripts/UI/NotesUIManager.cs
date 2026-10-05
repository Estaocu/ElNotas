using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesUIManager : MonoBehaviour
{
    [SerializeField] private Color[] colors;
    private List<UIPlayerNote> notes = new List<UIPlayerNote>();

    [SerializeField] private List<GameObject> lines = new List<GameObject>();

    [SerializeField] private PlayerRhythmController instrument;

    void Awake()
    {
        notes = new List<UIPlayerNote>(GetComponentsInChildren<UIPlayerNote>(true));
    }

    void Start()
    {
        foreach(GameObject line in lines)
        {
            line.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (instrument != null)
        {
            instrument.OnNoteAccepted += ProcessNote;
            instrument.OnMelodyCleared += CleanAllNotes;
        }
    }

    private void OnDisable()
    {
        if (instrument != null)
        {
            instrument.OnNoteAccepted -= ProcessNote;
            instrument.OnMelodyCleared -= CleanAllNotes;
        }
    }

    private void ProcessNote(PlayerRhythmController.PlayedNote playedNote)
    {
        notes[playedNote.position.subBeat].
        MatchPlayedNote(
        colors[(int)playedNote.note],
        (int)playedNote.note);

        //Debug.Log($"Shown slot {playedNote.position.subBeat + 1} with note {playedNote.note} and color {colors[(int)playedNote.note]}");

        foreach(GameObject line in lines)
        {
            line.SetActive(true);
        }
    }

    private void CleanAllNotes()
    {
        foreach (UIPlayerNote note in notes)
        {
            note.CleanNote();
        }

        foreach(GameObject line in lines)
        {
            line.SetActive(false);
        }
    }


}
