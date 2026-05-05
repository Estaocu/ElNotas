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
    private GameObject soundwavePrefab;
    private GameObject singleSw;
    private Instrument instrument;
    private SingerVoice voice;
    private LifeAndMeter lifeAndMeter;

    [Header("Settings")]
    [SerializeField] private Transform soundwaveSpawnpoint;
    [SerializeField] private float cooldown = 0.5f;

    [Space]

    [Header("NPC Rhythm Pattern")]
    [SerializeField] private PatternMode patternMode = PatternMode.Local;
    [SerializeField] private NoteSlot[] pattern = new NoteSlot[16];
    [SerializeField, Range(0f, 0.25f)] private float humanizationPercent = 0.05f;

    // ====== Estado runtime (Ocultos) ======
    private float lastSingTime = -999f;
    private readonly notesEnum[] noteBuffer = new notesEnum[4];
    private int notesPlayed;
    private bool isPatternActive;
    private bool waitingForStart;
    private int nextSlotIndex;
    private int slotsRemaining;

    public bool IsPlayer => instrument != null;
    public event System.Action<Melody> onSoundwaveSpawned;

    private void Awake()
    {
        soundwavePrefab = Resources.Load<GameObject>("Soundwave");
        singleSw = Resources.Load<GameObject>("SingleNoteSoundwave");
        database = Resources.Load<MelodyDatabase>("MelodyDatabase");
        Debug.Log(singleSw == null ? "SingleSw NULL" : "SingleSw OK");

        // Singleton access
        if (GameManager.Instance != null)
        {
            timingValidator = GameManager.Instance.RhythmTimingValidator;
        }

        // Auto-detect player vs NPC
        if (gameObject.CompareTag("Player"))
        {
            instrument = GetComponent<Instrument>();
            lifeAndMeter = GetComponent<LifeAndMeter>();
        }

        voice = GetComponent<SingerVoice>();

        // Safety check for spawnpoint
        if (soundwaveSpawnpoint == null) soundwaveSpawnpoint = transform;
    }

    private void OnValidate()
    {
        if (pattern == null || pattern.Length != 16)
        {
            var resized = new NoteSlot[16];
            if (pattern != null)
            {
                int copy = Mathf.Min(pattern.Length, 16);
                for (int i = 0; i < copy; i++) resized[i] = pattern[i];
            }
            pattern = resized;
        }
    }

    private void OnEnable()
    {
        if (instrument != null)
            instrument.OnNoteAdded += OnPlayerNoteAdded;
        else
            RhythmManager.OnBeatChanged += OnBeatChanged;
    }

    private void OnDisable()
    {
        if (instrument != null)
            instrument.OnNoteAdded -= OnPlayerNoteAdded;
        else
            RhythmManager.OnBeatChanged -= OnBeatChanged;
    }

    // ========================================================================
    // JUGADOR
    // ========================================================================

    private void OnPlayerNoteAdded(notesEnum[] sequence, int played)
    {
        if (voice != null)
            voice.PlayNote(sequence[3]);

        // Calculate timing reward
        double hitTime = AudioSettings.dspTime;
        int meterAward = 1;
        if (timingValidator != null)
        {
            meterAward = timingValidator.GetMeterAward(hitTime);
            if (meterAward > 0 && lifeAndMeter != null)
            {
                lifeAndMeter.ChangeMeterCharge(meterAward);
            }
        }

        if (database == null || soundwavePrefab == null) return;

        foreach (var melody in database.melodies)
        {
            if (melody == null || melody.notes == null || melody.notes.Length == 0) continue;

            int n = melody.notes.Length;
            if (played < n) continue;

            if (SequenceEndMatches(sequence, melody.notes))
            {
                TrySpawnSoundwave(melody);
                if (instrument != null) instrument.ClearSequence();
                return; // Una melodía por pulsación
            }
        }
    }

    // ========================================================================
    // NPC
    // ========================================================================

    // Disparo externo: arranca el patrón según el modo configurado.
    public void Sing()
    {
        if (IsPlayer) return;
        waitingForStart = true;
    }

    public void StopSinging()
    {
        waitingForStart = false;
        isPatternActive = false;
    }

    private void OnBeatChanged(int beatOf16)
    {
        if (IsPlayer) return;
        if (pattern == null || pattern.Length < 16) return;

        int nextSubBeat = beatOf16 >= 16 ? 1 : beatOf16 + 1;

        // 1. Alinear arranque. Procesamos aquí el subbeat próximo (lookahead).
        if (waitingForStart)
        {
            bool aligned =
                (patternMode == PatternMode.Local && IsWholeBeat(nextSubBeat)) ||
                (patternMode == PatternMode.Absolute && nextSubBeat == 1);

            if (!aligned) return;

            waitingForStart = false;
            isPatternActive = true;
            nextSlotIndex = 0;
            slotsRemaining = 16;
        }

        if (!isPatternActive) return;

        // 2. Programar el slot que debe sonar en el próximo subbeat.
        NoteSlot slot = pattern[nextSlotIndex];
        if (slot != NoteSlot.Empty)
        {
            var rm = RhythmManager.Instance;
            if (rm != null)
            {
                // NextSubBeatDspTime apunta al subbeat actual cuando se lee desde
                // dentro de OnBeatChanged (nextBeatTime se incrementa después del invoke).
                // Sumamos un SubBeatDuration para apuntar correctamente al siguiente.
                double target = rm.NextSubBeatDspTime + rm.SubBeatDuration
                                + Random.Range(-humanizationPercent, humanizationPercent)
                                  * rm.SubBeatDuration;

                StartCoroutine(PerformNoteAt((notesEnum)slot, target));
            }
        }

        // 3. Avanzar el cursor del patrón.
        nextSlotIndex++;
        slotsRemaining--;
        if (slotsRemaining <= 0)
            isPatternActive = false;
    }

    private static bool IsWholeBeat(int subBeat)
    {
        // Whole beats con 4 subbeats por beat: 1, 5, 9, 13.
        return ((subBeat - 1) % 4) == 0;
    }

    private IEnumerator PerformNoteAt(notesEnum note, double targetDspTime)
    {
        while (AudioSettings.dspTime < targetDspTime)
            yield return null;

        SingNote(note);
    }

    private void SingNote(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);

        // Shift-left e insertar al final (idéntico al buffer del Instrument).
        noteBuffer[0] = noteBuffer[1];
        noteBuffer[1] = noteBuffer[2];
        noteBuffer[2] = noteBuffer[3];
        noteBuffer[3] = note;
        if (notesPlayed < 4) notesPlayed++;

        // SpawnSingleNoteSoundwave(note);

        if (database == null || soundwavePrefab == null) return;

        foreach (var melody in database.melodies)
        {
            if (melody == null || melody.notes == null || melody.notes.Length == 0) continue;

            int n = melody.notes.Length;
            if (notesPlayed < n) continue;

            if (SequenceEndMatches(noteBuffer, melody.notes))
            {
                TrySpawnSoundwave(melody);
                ClearLocalBuffer();
                return;
            }
        }
    }

    // private void SpawnSingleNoteSoundwave(notesEnum note)
    // {
    //     Vector3 spawnPos = soundwaveSpawnpoint != null
    //     ? soundwaveSpawnpoint.position
    //     : transform.position;

    //     Debug.Log("Intentando spawn SingleNote");
    //     var instance = Instantiate(singleSw, spawnPos, Quaternion.identity);
    //     var miniSw = instance.GetComponent<SingleNoteSoundwave>();
    //     if (singleSw == null)
    //     {
    //         Debug.LogError("SingleNoteSoundwave prefab no cargado");
    //         return;
    //     }
    //     miniSw.Expand(note);
    //     Debug.Log("SingleNoteSW spawned: " + note);
    // }

    private void ClearLocalBuffer()
    {
        for (int i = 0; i < noteBuffer.Length; i++) noteBuffer[i] = default;
        notesPlayed = 0;
    }

    // ========================================================================
    // API COMÚN
    // ========================================================================

    // Empuja una nota externamente al flujo de canto NPC (sonido + buffer + match).
    public void AddNote(notesEnum note)
    {
        if (IsPlayer) return;
        SingNote(note);
    }

    // Tocar una nota sin generar soundwave.
    public void PlayNoteSound(notesEnum note)
    {
        if (voice != null)
            voice.PlayNote(note);
    }

    // Llamar directamente con la melodía ya decidida.
    public void SpawnSoundwave(Melody melody, notesEnum? note = null)
    {
        if (note.HasValue && voice != null)
            voice.PlayNote(note.Value);

        TrySpawnSoundwave(melody);
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

    private void TrySpawnSoundwave(Melody melody)
    {
        if (Time.time - lastSingTime < cooldown) return;
        lastSingTime = Time.time;

        Vector3 spawnPos = soundwaveSpawnpoint != null
            ? soundwaveSpawnpoint.position
            : transform.position;

        //Debug.Log($"[Singer] Melodía detectada: {melody?.melodyName ?? "DEBUG"} | Spawn pos: {spawnPos}");

        var instance = Instantiate(soundwavePrefab, spawnPos, Quaternion.identity);
        var soundwave = instance.GetComponent<Soundwave>();
        if (soundwave != null)
        {
            soundwave.myMelody = melody;
            soundwave.source = this;
        }

        onSoundwaveSpawned?.Invoke(melody);
    }
}