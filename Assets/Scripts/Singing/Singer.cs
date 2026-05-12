using System.Collections;
using UnityEngine;

public enum NoteSlot { Empty = -1, Note1 = 0, Note2 = 1, Note3 = 2, Note4 = 3 }
public enum PatternMode { Local, Absolute }

[RequireComponent(typeof(SingerVoice))]
public class Singer : MonoBehaviour
{
    // ====== Core References (Cargados en Awake) ======
    private RhythmTimingValidator timingValidator;
    private MelodyDatabase database;
    private Instrument instrument;
    private SingerVoice voice;
    private LifeAndMeter lifeAndMeter;

    [Header("Settings")]
    [SerializeField] private Transform soundwaveSpawnpoint;
    [SerializeField] private float cooldown = 0.2f;

    // ====== Estado runtime (Ocultos) ======
    private float lastSingTime = -999f;
    private readonly notesEnum[] noteBuffer = new notesEnum[4];
    private int notesPlayed;

    public bool IsPlayer => instrument != null;
    public event System.Action<Melody> onSoundwaveSpawned;

    private void Awake()
    {
        database = Resources.Load<MelodyDatabase>("MelodyDatabase");

        if (GameManager.Instance != null)
        {
            timingValidator = GameManager.Instance.RhythmTimingValidator;
        }

        if (gameObject.CompareTag("Player"))
        {
            instrument = GetComponent<Instrument>();
            lifeAndMeter = GetComponent<LifeAndMeter>();
        }

        voice = GetComponent<SingerVoice>();

        if (soundwaveSpawnpoint == null) soundwaveSpawnpoint = transform;
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
    // PROCESAMIENTO DE NOTAS
    // ========================================================================

    private void OnPlayerNoteAdded(notesEnum[] sequence, int played)
    {
        notesEnum note = sequence[3];
        
        // 1. Sonido y Onda Individual (SIEMPRE)
        ProcessSingleNote(note);

        // 2. Comprobar Melodías
        if (database == null) return;

        foreach (var melody in database.melodies)
        {
            if (melody == null || melody.notes == null || melody.notes.Length == 0) continue;

            int n = melody.notes.Length;
            if (played < n) continue;

            if (SequenceEndMatches(sequence, melody.notes))
            {
                TrySpawnMelody(melody);
                if (instrument != null) instrument.ClearSequence();
                return;
            }
        }
    }

    // Usado por NPCs (AddNotesOnBeat) u otros efectos
    public void AddNote(notesEnum note)
    {
        if (IsPlayer) return;

        // 1. Sonido y Onda Individual
        ProcessSingleNote(note);

        // 2. Buffer local para NPCs
        noteBuffer[0] = noteBuffer[1];
        noteBuffer[1] = noteBuffer[2];
        noteBuffer[2] = noteBuffer[3];
        noteBuffer[3] = note;
        if (notesPlayed < 4) notesPlayed++;

        // 3. Comprobar Melodías
        if (database == null) return;

        foreach (var melody in database.melodies)
        {
            if (melody == null || melody.notes == null || melody.notes.Length == 0) continue;

            int n = melody.notes.Length;
            if (notesPlayed < n) continue;

            if (SequenceEndMatches(noteBuffer, melody.notes))
            {
                TrySpawnMelody(melody);
                ClearLocalBuffer();
                return;
            }
        }
    }

    private void ProcessSingleNote(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);

        NoteSystem.EmitSingleNote(note, soundwaveSpawnpoint.position, gameObject);
    }

    private void TrySpawnMelody(Melody melody)
    {
        if (Time.time - lastSingTime < cooldown) return;
        lastSingTime = Time.time;

        NoteSystem.EmitMelody(melody, soundwaveSpawnpoint.position, this);
        onSoundwaveSpawned?.Invoke(melody);
    }

    private void ClearLocalBuffer()
    {
        for (int i = 0; i < noteBuffer.Length; i++) noteBuffer[i] = default;
        notesPlayed = 0;
    }

    private bool SequenceEndMatches(notesEnum[] sequence, notesEnum[] melodyNotes)
    {
        int n = melodyNotes.Length;
        int startIndex = 4 - n;

        for (int i = 0; i < n; i++)
        {
            if (sequence[startIndex + i] != melodyNotes[i])
                return false;
        }
        return true;
    }

    // Tocar una nota sin generar nada (solo sonido)
    public void PlayNoteSoundOnly(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);
    }
}