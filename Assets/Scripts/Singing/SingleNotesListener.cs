using Unity.VisualScripting;
using UnityEngine;

public class SingleNotesListener : MonoBehaviour
{

    [Header ("Melodies")]
    [SerializeField] private notesEnum[] currentMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] externalMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] desiredMelody = new notesEnum[2];
    private int storedNotes = 0;
    private bool wantsToListen = true;
    private IControllableProjectile homingProjectile;
    private SingleNoteSoundwave sw;

    void Awake()
    {
        homingProjectile = transform.parent.GetComponent<IControllableProjectile>();
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
            AddNote(sw.myNote, currentMelody); //A pelo, se añade y ya
            break;

            case 1:
            CompareWithFirstNote(sw.myNote); // Caution porque hay que comprobar que la nueva nota no sea la misma que la vieja
            break;

            case 2: // Se podría decir que pasa de modo "recording" a modo "requesting"
            AddNote(sw.myNote, externalMelody);
            CompareMelodies();
            break;


        }
    }

    private void AddNote(notesEnum newNote, notesEnum[] whichArray)
{
    for (int i = 0; i < whichArray.Length; i++)
    {
        if (whichArray[i] == default) // default es más seguro que 0
        {
            whichArray[i] = newNote;

            if (whichArray == externalMelody) return;

            storedNotes = Mathf.Min(storedNotes + 1, 2);
            return;
        }
    }
}

    private void CompareWithFirstNote(notesEnum newNote)
{
    if (newNote == currentMelody[0])
    {
        ClearMelody(currentMelody); // Esto pondrá storedNotes = 0
        Debug.Log("Nota repetida. Maiz desinflado.");
        return;   
    }

    currentMelody[1] = newNote;

    desiredMelody[0] = currentMelody[0];
    desiredMelody[1] = currentMelody[1];

    // Debug.Log($"Melodía guardada: {desiredMelody[0]} e {desiredMelody[1]}");


    ClearMelody(currentMelody);
    storedNotes = 2;

    // homingProjectile.SetMovementMode(Mode.Homing, sw.author);
    homingProjectile.Launch(sw.author);
    Debug.Log("Maíz lanzado por " + sw.author);
}

    private void CompareMelodies()
    {
        int lastNoteIndex = (externalMelody[1] == default) ? 0 : 1;
        notesEnum noteToVerify = externalMelody[lastNoteIndex];

        bool isCorrect = false;
        if (lastNoteIndex == 0 && noteToVerify == desiredMelody[1]) isCorrect = true;
        else if (lastNoteIndex == 1 && noteToVerify == desiredMelody[0]) isCorrect = true;

        if (isCorrect)
        {
            Debug.Log($"Nota {lastNoteIndex + 1} correcta.");
            
            // Si las 2 son buenas
            if (lastNoteIndex == 1)
            {
                // homingProjectile.SetMovementMode(Mode.Forward, sw.author);
                homingProjectile.Launch(sw.author);

                RearrangeMelody(desiredMelody);
                ClearMelody(externalMelody);
                //Debug.Log("MAIZ REDIRIGIDO");
            }
        }
        else
        {
            ClearMelody(externalMelody);
            //Debug.Log("Nota incorrecta, ignorada");

        }
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
