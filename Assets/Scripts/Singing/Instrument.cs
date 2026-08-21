using System;
using CMF;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Instrument : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRhythmController playerRhythmController;

    private Singer singer;
    private PlayerInputs input;

    public ActionMapsManager controlsManager;

    [SerializeField] public TMP_Text[] uiNotes;

    // The four most recent accepted notes.
    public notesEnum[] noteSequence = new notesEnum[4];

    // Number of accepted notes currently stored.
    public int notesPlayed { get; private set; }

    // Singer subscribes to this event to check melodies.
    public event Action<notesEnum[], int> OnNoteAdded;

    private void Awake()
    {
        input = new PlayerInputs();

        singer = GetComponent<Singer>();

        if (playerRhythmController == null)
            playerRhythmController = GetComponent<PlayerRhythmController>();
    }

    private void OnEnable()
    {
        input.Gameplay.Note1.performed += OnNote1Played;
        input.Gameplay.Note2.performed += OnNote2Played;
        input.Gameplay.Note3.performed += OnNote3Played;
        input.Gameplay.Note4.performed += OnNote4Played;

        input.Gameplay.Enable();

        if (playerRhythmController != null)
            playerRhythmController.OnMelodyCleared += ClearSequence;
    }

    private void OnDisable()
    {
        input.Gameplay.Note1.performed -= OnNote1Played;
        input.Gameplay.Note2.performed -= OnNote2Played;
        input.Gameplay.Note3.performed -= OnNote3Played;
        input.Gameplay.Note4.performed -= OnNote4Played;

        input.Gameplay.Disable();

        if (playerRhythmController != null)
            playerRhythmController.OnMelodyCleared -= ClearSequence;
    }

    private void TryPlayNote(notesEnum note)
    {
        if (playerRhythmController == null)
        {
            Debug.LogError(
                "Instrument requires a PlayerRhythmController reference."
            );

            return;
        }

        bool accepted =
            playerRhythmController.ProcessNote(note);

        if (!accepted)
            return;

        AddNote(note);
    }

    private void AddNote(notesEnum note)
    {
        if (notesPlayed < 4)
        {
            noteSequence[notesPlayed] = note;
            notesPlayed++;
        }
        else
        {
            noteSequence[0] = noteSequence[1];
            noteSequence[1] = noteSequence[2];
            noteSequence[2] = noteSequence[3];
            noteSequence[3] = note;
        }

        UpdateUIDisplays();

        OnNoteAdded?.Invoke(noteSequence, notesPlayed);
    }

    public void ClearSequence()
    {
        for (int i = 0; i < noteSequence.Length; i++)
            noteSequence[i] = default;

        notesPlayed = 0;

        UpdateUIDisplays();
    }

    private void UpdateUIDisplays()
    {
        for (int i = 0; i < uiNotes.Length; i++)
        {
            if (uiNotes[i] == null)
                continue;

            if (i < notesPlayed)
            {
                uiNotes[i].text =
                    ((int)noteSequence[i] + 1).ToString();
            }
            else
            {
                uiNotes[i].text = "";
            }
        }
    }

    private void OnNote1Played(InputAction.CallbackContext context)
    {
        TryPlayNote(notesEnum.Note1);
    }

    private void OnNote2Played(InputAction.CallbackContext context)
    {
        TryPlayNote(notesEnum.Note2);
    }

    private void OnNote3Played(InputAction.CallbackContext context)
    {
        TryPlayNote(notesEnum.Note3);
    }

    private void OnNote4Played(InputAction.CallbackContext context)
    {
        TryPlayNote(notesEnum.Note4);
    }
}