using System;
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
    private Instrument instrument;
    private PlayerRhythmController playerRhythmController;
    private SingerVoice voice;

    [Header("Settings")]
    [SerializeField] private Transform soundwaveSpawnpoint;
    [SerializeField] private float cooldown = 0.2f;

    private float lastSingTime = -999f;

    private readonly notesEnum[] noteBuffer = new notesEnum[4];
    private int notesPlayed;

    public bool IsPlayer => instrument != null;

    public event Action<Melody> onSoundwaveSpawned;

    private void Awake()
    {
        database = Resources.Load<MelodyDatabase>("MelodyDatabase");

        if (GameManager.Instance != null)
            timingValidator = GameManager.Instance.RhythmTimingValidator;

        if (gameObject.CompareTag("Player"))
        {
            instrument = GetComponent<Instrument>();
            playerRhythmController = GetComponent<PlayerRhythmController>();
        }

        voice = GetComponent<SingerVoice>();

        if (soundwaveSpawnpoint == null)
            soundwaveSpawnpoint = transform;
    }

    private void OnEnable()
    {
        if (instrument != null)
            instrument.OnNoteAdded += OnPlayerNoteAdded;
    }

    private void OnDisable()
    {
        if (instrument != null)
            instrument.OnNoteAdded -= OnPlayerNoteAdded;
    }

    // ========================================================================
    // PLAYER
    // ========================================================================

    private void OnPlayerNoteAdded(notesEnum[] sequence, int played)
    {
        if (sequence == null || played <= 0)
            return;

        notesEnum note = sequence[3];

        // Play the individual note.
        ProcessSingleNote(note);

        // Check melodies.
        if (database == null)
            return;

        foreach (Melody melody in database.melodies)
        {
            if (melody == null ||
                melody.notes == null ||
                melody.notes.Length == 0)
            {
                continue;
            }

            int melodyLength = melody.notes.Length;

            if (played < melodyLength)
                continue;

            if (!SequenceEndMatches(sequence, melody.notes))
                continue;

            TrySpawnPlayerMelody(melody);
            return;
        }
    }

    // ========================================================================
    // NPC / NON-PLAYER SINGERS
    // ========================================================================

    public void AddNote(notesEnum note)
    {
        if (IsPlayer)
            return;

        ProcessSingleNote(note);

        noteBuffer[0] = noteBuffer[1];
        noteBuffer[1] = noteBuffer[2];
        noteBuffer[2] = noteBuffer[3];
        noteBuffer[3] = note;

        if (notesPlayed < 4)
            notesPlayed++;

        if (database == null)
            return;

        foreach (Melody melody in database.melodies)
        {
            if (melody == null ||
                melody.notes == null ||
                melody.notes.Length == 0)
            {
                continue;
            }

            int melodyLength = melody.notes.Length;

            if (notesPlayed < melodyLength)
                continue;

            if (!SequenceEndMatches(noteBuffer, melody.notes))
                continue;

            TrySpawnMelody(melody);
            ClearLocalBuffer();
            return;
        }
    }

    // ========================================================================
    // NOTE PROCESSING
    // ========================================================================

    private void ProcessSingleNote(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);

        NoteSystem.EmitSingleNote(
            note,
            soundwaveSpawnpoint.position,
            gameObject
        );
    }

    // ========================================================================
    // PLAYER MELODY
    // ========================================================================

    private void TrySpawnPlayerMelody(Melody melody)
    {
        if (playerRhythmController == null)
            return;

        // A valid melody can only become a soundwave when the meter is full.
        if (!playerRhythmController.HasFullMeter)
            return;

        // Consume the meter only if the soundwave can actually be spawned.
        if (!playerRhythmController.TryConsumeFullMeter())
            return;

        TrySpawnMelody(melody);

        // The melody has successfully been cast.
        instrument?.ClearSequence();
    }

    // ========================================================================
    // MELODY SPAWNING
    // ========================================================================

    private void TrySpawnMelody(Melody melody)
    {
        if (Time.time - lastSingTime < cooldown)
            return;

        lastSingTime = Time.time;

        NoteSystem.EmitMelody(
            melody,
            soundwaveSpawnpoint.position,
            this
        );

        onSoundwaveSpawned?.Invoke(melody);
    }

    // ========================================================================
    // BUFFER
    // ========================================================================

    private void ClearLocalBuffer()
    {
        for (int i = 0; i < noteBuffer.Length; i++)
            noteBuffer[i] = default;

        notesPlayed = 0;
    }

    private bool SequenceEndMatches(
        notesEnum[] sequence,
        notesEnum[] melodyNotes)
    {
        int melodyLength = melodyNotes.Length;

        if (melodyLength > 4)
            return false;

        int startIndex = 4 - melodyLength;

        for (int i = 0; i < melodyLength; i++)
        {
            if (sequence[startIndex + i] != melodyNotes[i])
                return false;
        }

        return true;
    }

    // ========================================================================
    // AUDIO ONLY
    // ========================================================================

    public void PlayNoteSoundOnly(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);
    }
}