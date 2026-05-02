using UnityEngine;

public class SingleNotesListener : MonoBehaviour
{

    [Header ("Melodies")]
    [SerializeField] private notesEnum[] currentMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] externalMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] desiredMelody = new notesEnum[2];
    private int storedNotes = 0;

    [Space]

    [Header("Settings")]
    [SerializeField] private float explodeTime = 5f;
    [SerializeField] private float regrowthCooldown = 10f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private int speedIncreaseRatio = 20; // En porcentaje I guess
    private GameObject target;

    private bool wantsToListen = true;
    //modes: recording, requesting
    //record record --> launch y request

    void OnTriggerEnter(Collider other)
    {
        SingleNoteSoundwave sw = other.GetComponent<SingleNoteSoundwave>();
        if (sw == null || !wantsToListen ) return;

        Debug.Log("Me ha entrado una nota");

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

    Debug.Log($"Melodía guardada: {desiredMelody[0]} e {desiredMelody[1]}");


    ClearMelody(currentMelody);
    storedNotes = 2; 
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
                Debug.Log("MAIZ REDIRIGIDO");
                RearrangeMelody(desiredMelody);
                ClearMelody(externalMelody);
            }
        }
        else
        {
            // Si falla aunque sea la primera nota
            Debug.Log("Melody mismatch! Exploding immediately.");
            ClearMelody(externalMelody);
            wantsToListen = false;
            // Explosion

        }

        
        // if (externalMelody[1] == default) return;
        // if (desiredMelody[0] == externalMelody[1] && desiredMelody[1] == externalMelody[0])
        // {
        //     //Redirigir maiz
        //     Debug.Log("MAIZ REDIRIGIDO");
        //     RearrangeMelody(desiredMelody);
        //     ClearMelody(externalMelody);
        // }
        // else
        // {
        //     Debug.Log("Melody not equal! Exploding on target.");
        //     ClearMelody(externalMelody);
        // }
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
