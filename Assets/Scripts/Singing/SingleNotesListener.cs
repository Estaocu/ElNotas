using Unity.VisualScripting;
using UnityEngine;

public class SingleNotesListener : MonoBehaviour, IPoolable
{

    [Header ("Melodies")]
    [SerializeField] private notesEnum[] currentMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] externalMelody = new notesEnum[2];
    public notesEnum[] desiredMelody = new notesEnum[2];
    private int storedNotes = 0;
    private int externalNotesStored = 0;
    private bool wantsToListen = true;
    private IControllableProjectile homingProjectile;
    private SingleNoteSoundwave sw;

    void Awake()
    {
        homingProjectile = transform.parent.GetComponent<IControllableProjectile>();
    }

    public void OnSpawn()
    {
        ClearMelody(currentMelody);
        ClearMelody(externalMelody);
        ClearMelody(desiredMelody);
        storedNotes = 0;
        externalNotesStored = 0;
        wantsToListen = true;
        sw = null;
    }

    void OnTriggerEnter(Collider other)
    {
        sw = other.GetComponent<SingleNoteSoundwave>();
        if (sw == null || !wantsToListen ) return;

        // Debug.Log("Me ha entrado una nota");
        homingProjectile.SaveNotePosition(sw.author.transform);

     

        switch (storedNotes)
        {
            case 0:
            AddCurrentNote(sw.myNote);
            break;

            case 1:
            CompareWithFirstNote(sw.myNote); // Caution porque hay que comprobar que la nueva nota no sea la misma que la vieja
            break;

            case 2: // Se podría decir que pasa de modo "recording" a modo "requesting"
            AddExternalNote(sw.myNote);
            CompareMelodies();
            break;


        }
    }

    private void AddCurrentNote(notesEnum note)
    {
        if (storedNotes >= currentMelody.Length) return;
        currentMelody[storedNotes] = note;
        storedNotes++;
    }

    private void AddExternalNote(notesEnum note)
    {
        if (externalNotesStored >= externalMelody.Length) return;
        externalMelody[externalNotesStored] = note;
        externalNotesStored++;
    }

    private void CompareWithFirstNote(notesEnum newNote)
{
    if (newNote == currentMelody[0])
    {
        ClearMelody(currentMelody);
        storedNotes = 0;
        Debug.Log("Nota repetida. Maiz desinflado.");
        return;
    }

    currentMelody[1] = newNote;

    desiredMelody[0] = currentMelody[0];
    desiredMelody[1] = currentMelody[1];

    // Debug.Log($"Melodía guardada: {desiredMelody[0]} e {desiredMelody[1]}");


    ClearMelody(currentMelody);
    storedNotes = 2;

    homingProjectile.Launch(sw.author);
    Debug.Log("Maíz lanzado por " + sw.author);
}

    private void CompareMelodies()
    {
        int lastNoteIndex = externalNotesStored - 1;
        if (lastNoteIndex < 0) return;
        notesEnum noteToVerify = externalMelody[lastNoteIndex];

        bool isCorrect = false;
        if (lastNoteIndex == 0 && noteToVerify == desiredMelody[1]) isCorrect = true;
        else if (lastNoteIndex == 1 && noteToVerify == desiredMelody[0]) isCorrect = true;

        if (isCorrect)
        {
            Debug.Log($"Nota {lastNoteIndex + 1} correcta.");

            if (lastNoteIndex == 1)
            {
                homingProjectile.Launch(sw.author);
                RearrangeMelody(desiredMelody);
                ClearExternalMelody();
            }
        }
        else
        {
            ClearExternalMelody();
        }
    }

    private void ClearExternalMelody()
    {
        ClearMelody(externalMelody);
        externalNotesStored = 0;
    }

    private void RearrangeMelody(notesEnum[] melody)
    {
        (desiredMelody[0], desiredMelody[1]) = (desiredMelody[1], desiredMelody[0]);
        Debug.Log("Required melody flipped");
    }

    private void ClearMelody(notesEnum[] whichMelody)
    {
        for (int i = 0; i < whichMelody.Length; i++) whichMelody[i] = default;
    }
    
}
