using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMelodyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRhythmController playerRhythmController;

    [Header("Melodies")]
    [SerializeField] private Melody[] melodies;

    [Header("Meter")]
    [SerializeField] private float fullMeterTolerance = 0.01f;

    // Called when a valid melody is completed but the meter is not full.
    public event Action<Melody> OnMelodyWithoutEnoughMeter;

    // Called when a valid melody is completed and the meter is consumed.
    public event Action<Melody> OnMelodyTriggered;

    private void Awake()
    {
        if (playerRhythmController == null)
        {
            playerRhythmController =
                GetComponent<PlayerRhythmController>();
        }
    }

    private void OnEnable()
    {
        if (playerRhythmController != null)
        {
            playerRhythmController.OnNoteAccepted += HandleNoteAccepted;
        }
    }

    private void OnDisable()
    {
        if (playerRhythmController != null)
        {
            playerRhythmController.OnNoteAccepted -= HandleNoteAccepted;
        }
    }

    private void HandleNoteAccepted(
        PlayerRhythmController.PlayedNote playedNote)
    {
        IReadOnlyList<PlayerRhythmController.PlayedNote> notes =
            playerRhythmController.CurrentNotes;

        if (notes.Count != 4)
            return;

        Melody melody =
            FindMatchingMelody(notes);

        if (melody == null)
            return;

        if (!HasFullMeter())
        {
            playerRhythmController.ClearMelody();

            OnMelodyWithoutEnoughMeter?.Invoke(
                melody
            );

            return;
        }

        playerRhythmController.SetMeter(0.0f);
        playerRhythmController.ClearMelody();

        OnMelodyTriggered?.Invoke(
            melody
        );
    }

    private Melody FindMatchingMelody(
        IReadOnlyList<PlayerRhythmController.PlayedNote> notes)
    {
        if (melodies == null)
            return null;

        for (int i = 0; i < melodies.Length; i++)
        {
            Melody melody = melodies[i];

            if (melody == null ||
                melody.notes == null ||
                melody.notes.Length != 4)
            {
                continue;
            }

            if (MatchesMelody(notes, melody))
                return melody;
        }

        return null;
    }

    private bool MatchesMelody(
        IReadOnlyList<PlayerRhythmController.PlayedNote> notes,
        Melody melody)
    {
        for (int i = 0; i < 4; i++)
        {
            if (notes[i].note != melody.notes[i])
                return false;
        }

        return true;
    }

    private bool HasFullMeter()
    {
        return playerRhythmController.CurrentMeter >=
               playerRhythmController.MaxMeter -
               fullMeterTolerance;
    }
}