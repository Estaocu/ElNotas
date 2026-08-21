using System;
using System.Collections.Generic;
using UnityEngine;

public enum NoteSlot
{
    Empty = -1,
    Note1 = 0,
    Note2 = 1,
    Note3 = 2,
    Note4 = 3
}

public enum PatternMode
{
    Local,
    Absolute
}

[RequireComponent(typeof(SingerVoice))]
public class Singer : MonoBehaviour
{
    private RhythmTimingValidator timingValidator;
    private MelodyDatabase database;
    private PlayerRhythmController playerRhythmController;
    private SingerVoice voice;

    [Header("Settings")]
    [SerializeField] private Transform soundwaveSpawnpoint;
    [SerializeField] private float cooldown = 0.2f;

    private float lastSingTime = -999f;

    private readonly notesEnum[] noteBuffer = new notesEnum[4];
    private int notesPlayed;

    public bool IsPlayer => playerRhythmController != null;

    public event Action<Melody> onSoundwaveSpawned;

    private void Awake()
    {
        database = Resources.Load<MelodyDatabase>("MelodyDatabase");

        if (GameManager.Instance != null)
        {
            timingValidator = GameManager.Instance.RhythmTimingValidator;
        }

        playerRhythmController =
            GetComponent<PlayerRhythmController>();

        voice = GetComponent<SingerVoice>();

        if (soundwaveSpawnpoint == null)
        {
            soundwaveSpawnpoint = transform;
        }
    }

    private void OnEnable()
    {
        if (playerRhythmController != null)
        {
            playerRhythmController.OnNoteAccepted += OnPlayerNoteAccepted;
        }
    }

    private void OnDisable()
    {
        if (playerRhythmController != null)
        {
            playerRhythmController.OnNoteAccepted -= OnPlayerNoteAccepted;
        }
    }

    private void OnPlayerNoteAccepted(
        PlayerRhythmController.PlayedNote playedNote)
    {
        notesEnum note = playedNote.note;

        ProcessSingleNote(note);

        if (database == null ||
            database.melodies == null ||
            database.melodies.Length == 0)
        {
            return;
        }

        CheckPlayerMelodies();
    }

    private void CheckPlayerMelodies()
    {
        IReadOnlyList<PlayerRhythmController.PlayedNote> notes =
            playerRhythmController.CurrentNotes;

        if (notes.Count < 4)
        {
            return;
        }

        for (int i = 0; i < database.melodies.Length; i++)
        {
            Melody melody = database.melodies[i];

            if (melody == null ||
                melody.notes == null ||
                melody.notes.Length == 0)
            {
                continue;
            }

            if (notes.Count < melody.notes.Length)
            {
                continue;
            }

            if (!SequenceMatches(notes, melody.notes))
            {
                continue;
            }

            TrySpawnPlayerMelody(melody);
            return;
        }
    }

    private bool SequenceMatches(
        IReadOnlyList<PlayerRhythmController.PlayedNote> notes,
        notesEnum[] melodyNotes)
    {
        int startIndex =
            notes.Count - melodyNotes.Length;

        for (int i = 0; i < melodyNotes.Length; i++)
        {
            if (notes[startIndex + i].note != melodyNotes[i])
            {
                return false;
            }
        }

        return true;
    }

    private void TrySpawnPlayerMelody(Melody melody)
    {
        if (!playerRhythmController.HasFullMeter)
        {
            return;
        }

        if (Time.time - lastSingTime < cooldown)
        {
            return;
        }

        if (!playerRhythmController.ConsumeFullMeter())
        {
            return;
        }

        lastSingTime = Time.time;

        NoteSystem.EmitMelody(
            melody,
            soundwaveSpawnpoint.position,
            this
        );

        onSoundwaveSpawned?.Invoke(melody);

        playerRhythmController.ClearMelody();
    }

    // Used by NPCs and other non-player singers.
    public void AddNote(notesEnum note)
    {
        if (IsPlayer)
        {
            return;
        }

        ProcessSingleNote(note);

        noteBuffer[0] = noteBuffer[1];
        noteBuffer[1] = noteBuffer[2];
        noteBuffer[2] = noteBuffer[3];
        noteBuffer[3] = note;

        if (notesPlayed < 4)
        {
            notesPlayed++;
        }

        CheckNpcMelodies();
    }

    private void CheckNpcMelodies()
    {
        if (database == null ||
            database.melodies == null ||
            database.melodies.Length == 0)
        {
            return;
        }

        for (int i = 0; i < database.melodies.Length; i++)
        {
            Melody melody = database.melodies[i];

            if (melody == null ||
                melody.notes == null ||
                melody.notes.Length == 0)
            {
                continue;
            }

            if (notesPlayed < melody.notes.Length)
            {
                continue;
            }

            if (!SequenceEndMatches(
                    noteBuffer,
                    melody.notes))
            {
                continue;
            }

            TrySpawnMelody(melody);
            ClearLocalBuffer();
            return;
        }
    }

    private void ProcessSingleNote(notesEnum note)
    {
        if (voice != null)
        {
            voice.PlayNote(note);
        }

        NoteSystem.EmitSingleNote(
            note,
            soundwaveSpawnpoint.position,
            gameObject
        );
    }

    private void TrySpawnMelody(Melody melody)
    {
        if (Time.time - lastSingTime < cooldown)
        {
            return;
        }

        lastSingTime = Time.time;

        NoteSystem.EmitMelody(
            melody,
            soundwaveSpawnpoint.position,
            this
        );

        onSoundwaveSpawned?.Invoke(melody);
    }

    private void ClearLocalBuffer()
    {
        for (int i = 0; i < noteBuffer.Length; i++)
        {
            noteBuffer[i] = default;
        }

        notesPlayed = 0;
    }

    private bool SequenceEndMatches(
        notesEnum[] sequence,
        notesEnum[] melodyNotes)
    {
        int startIndex =
            4 - melodyNotes.Length;

        if (startIndex < 0)
        {
            return false;
        }

        for (int i = 0; i < melodyNotes.Length; i++)
        {
            if (sequence[startIndex + i] != melodyNotes[i])
            {
                return false;
            }
        }

        return true;
    }

    public void PlayNoteSoundOnly(notesEnum note)
    {
        if (voice != null)
        {
            voice.PlayNote(note);
        }
    }
}