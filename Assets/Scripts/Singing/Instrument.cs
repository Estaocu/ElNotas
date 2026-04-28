using System;
using CMF;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Instrument : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Singer singer;
    private PlayerInputs input;
    public ActionMapsManager controlsManager;
    [SerializeField] private int timeToClearMelody;
    private BeatWaitHandle afkHandle;
    
    [SerializeField] public TMP_Text[] uiNotes;

    // Las últimas 4 notas tocadas. El índice 3 es siempre la más reciente.
    public notesEnum[] noteSequence = new notesEnum[4];

    // Cuántas notas reales ha tocado el jugador (máx 4).
    public int notesPlayed { get; private set; }

    // Singer se suscribe a este evento para comprobar melodías.
    public event Action<notesEnum[], int> OnNoteAdded;

    private void Awake()
    {
        input = new PlayerInputs();

    }

    private void OnEnable()
    {
        input.Gameplay.Note1.performed += OnNote1Played;
        input.Gameplay.Note2.performed += OnNote2Played;
        input.Gameplay.Note3.performed += OnNote3Played;
        input.Gameplay.Note4.performed += OnNote4Played;
        // input.Gameplay.DebugSpawnSoundwave.performed += OnDebugSpawnSoundwave;

        input.Gameplay.Enable();
    }

    private void OnDisable()
    {
        input.Gameplay.Note1.performed -= OnNote1Played;
        input.Gameplay.Note2.performed -= OnNote2Played;
        input.Gameplay.Note3.performed -= OnNote3Played;
        input.Gameplay.Note4.performed -= OnNote4Played;
        // input.Gameplay.DebugSpawnSoundwave.performed -= OnDebugSpawnSoundwave;

        input.Gameplay.Disable();
    }

    private void AddNote(notesEnum note)
    {
        afkHandle?.Cancel();
        // Desplaza las notas hacia la izquierda y coloca la nueva al final.
        noteSequence[0] = noteSequence[1];
        noteSequence[1] = noteSequence[2];
        noteSequence[2] = noteSequence[3];
        noteSequence[3] = note;

        if (notesPlayed < 4) notesPlayed++;

        // Debug.Log($"Nota tocada: {note} | Secuencia: [{noteSequence[0]}, {noteSequence[1]}, {noteSequence[2]}, {noteSequence[3]}]");

        UpdateUIDisplays();
        OnNoteAdded?.Invoke(noteSequence, notesPlayed);

        afkHandle = RhythmBeatWaiter.WaitForSubBeats(timeToClearMelody, BeatWaitMode.Immediate, ClearSequence);
    }

    // Llamado por Singer tras detectar una melodía y spawnear la soundwave,
    // para que la última nota no arrastre hacia el siguiente match.
    public void ClearSequence()
    {
        for (int i = 0; i < noteSequence.Length; i++) noteSequence[i] = default;
        notesPlayed = 0;
        UpdateUIDisplays();
        Debug.Log("Notes Cleared");
    }

    private void UpdateUIDisplays()
    {
        for (int i = 0; i < uiNotes.Length; i++)
        {
            if (uiNotes[i] != null)
            {
                if (i < notesPlayed)
                {
                    // Calcular el índice de origen: mientras notesPlayed < 4,
                    // las notas se alinean a la izquierda en la UI
                    int sourceIndex = 4 - notesPlayed + i;
                    uiNotes[i].text = ((int)noteSequence[sourceIndex] + 1).ToString();
                }
                else
                {
                    uiNotes[i].text = "";
                }
            }
        }
    }

    private void OnNote1Played(InputAction.CallbackContext context) => AddNote(notesEnum.Note1);
    private void OnNote2Played(InputAction.CallbackContext context) => AddNote(notesEnum.Note2);
    private void OnNote3Played(InputAction.CallbackContext context) => AddNote(notesEnum.Note3);
    private void OnNote4Played(InputAction.CallbackContext context) => AddNote(notesEnum.Note4);

    // private void OnDebugSpawnSoundwave(InputAction.CallbackContext context)
    // {
    //     if (singer != null)
    //         singer.SpawnSoundwave(null);
    // }
}
