using TMPro;
using UnityEngine;

public class NoteDisplayUI : MonoBehaviour
{
    [SerializeField] private Instrument instrument;
    [SerializeField] private TextMeshProUGUI[] noteSlots = new TextMeshProUGUI[4];

    private void OnEnable()
    {
        if (instrument != null)
            instrument.OnNoteAdded += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (instrument != null)
            instrument.OnNoteAdded -= UpdateDisplay;
    }

    private void UpdateDisplay(notesEnum[] sequence, int notesPlayed)
    {
        for (int i = 0; i < noteSlots.Length; i++)
        {
            if (noteSlots[i] == null) continue;

            // Los slots que aún no tienen nota real se muestran como guión.
            int notesAgo = 4 - (i + 1); // cuántas posiciones atrás está este slot
            bool hasRealNote = notesPlayed > notesAgo;

            noteSlots[i].text = hasRealNote ? NoteToDisplay(sequence[i]) : "-";
        }
    }

    private string NoteToDisplay(notesEnum note)
    {
        return note switch
        {
            notesEnum.Note1 => "1",
            notesEnum.Note2 => "2",
            notesEnum.Note3 => "3",
            notesEnum.Note4 => "4",
            _ => "-"
        };
    }
}
