using UnityEngine;

public class EntityRhythmPattern : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Singer singer;
    [SerializeField] private Melody melody;

    [Header("Pattern")]
    [Tooltip("Secuencia rítmica. Cada entrada define el spacing hasta la siguiente nota.")]
    [SerializeField] private PatternEntry[] pattern;
    [SerializeField] private bool looping = true;

    [Header("Humanization")]
    [Tooltip("Offset aleatorio máximo como fracción del beatDuration (ej: 0.05 = ±5%).")]
    [SerializeField] [Range(0f, 0.15f)] private float humanizationPercent = 0.05f;

    private int currentEntryIndex;
    private double nextAttackDspTime;
    private bool isActive;

    // Melody execution tracking
    private Melody currentMelodyBeingSung;
    private bool isExecutingMelody;
    private int notesPlayedInMelody;

    private void OnEnable()
    {
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnMeasureStart += OnMeasureStart;
            RhythmManager.Instance.OnResumed += OnResumed;
        }
    }

    private void OnDisable()
    {
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnMeasureStart -= OnMeasureStart;
            RhythmManager.Instance.OnResumed -= OnResumed;
        }
    }

    private void Update()
    {
        if (!isActive || pattern == null || pattern.Length == 0) return;
        if (RhythmManager.Instance == null || !RhythmManager.Instance.IsPlaying) return;
        if (RhythmManager.Instance.IsPaused) return;

        double now = AudioSettings.dspTime;
        if (now < nextAttackDspTime) return;

        PatternEntry entry = pattern[currentEntryIndex];

        if (!entry.isRest && singer != null)
        {
            if (isExecutingMelody)
            {
                // Durante ejecución de melodía personalizada: solo tocar la nota
                singer.PlayNoteSound(entry.note);
            }
            else
            {
                // Patrón regular: spawnea soundwave con la nota
                singer.SpawnSoundwave(melody, entry.note);
            }
        }

        // Si estamos ejecutando una melodía, rastrear el progreso
        if (isExecutingMelody)
        {
            notesPlayedInMelody++;

            // Si es la última nota de la melodía, spawneara soundwave
            if (notesPlayedInMelody >= currentMelodyBeingSung.notes.Length)
            {
                Debug.Log($"[EntityRhythmPattern] Melodía completada: {currentMelodyBeingSung.melodyName}. Spawnweando soundwave.");
                singer.SpawnSoundwave(currentMelodyBeingSung);
                isExecutingMelody = false;
                currentMelodyBeingSung = null;
            }
        }

        // Programar el siguiente ataque
        double spacingBeats = entry.BeatsConsumed;
        nextAttackDspTime += spacingBeats * RhythmManager.Instance.BeatDuration;

        // Aplicar humanización al próximo ataque (no acumulativa)
        double humanOffset = Random.Range(-humanizationPercent, humanizationPercent)
                             * RhythmManager.Instance.BeatDuration;
        nextAttackDspTime += humanOffset;

        currentEntryIndex++;
        if (currentEntryIndex >= pattern.Length)
        {
            if (looping && !isExecutingMelody)
                currentEntryIndex = 0;
            else if (!isExecutingMelody)
                isActive = false;
            else if (isExecutingMelody)
                isActive = false; // Stop after melody completes
        }
    }

    /// <summary>
    /// Inicia la ejecución de una melodía. Convierte las notas en un patrón rítmico y comienza a cantar.
    /// </summary>
    public void StartSingingMelody(Melody melody)
    {
        if (melody == null || melody.notes == null || melody.notes.Length == 0)
        {
            Debug.LogWarning("[EntityRhythmPattern] Se intentó cantar una melodía nula o vacía.", this);
            return;
        }

        currentMelodyBeingSung = melody;
        isExecutingMelody = true;
        notesPlayedInMelody = 0;

        // Convertir la melodía en un patrón temporal: una nota por beat
        PatternEntry[] tempPattern = new PatternEntry[melody.notes.Length];
        for (int i = 0; i < melody.notes.Length; i++)
        {
            tempPattern[i] = new PatternEntry
            {
                spacing = SubdivisionType.Quarter, // 1 beat per note
                isRest = false,
                note = melody.notes[i]
            };
        }

        // Reemplazar patrón temporalmente
        pattern = tempPattern;
        StartPattern();

        Debug.Log($"[EntityRhythmPattern] Iniciando melodía: {melody.melodyName} ({melody.notes.Length} notas)");
    }

    /// <summary>
    /// Inicia el patrón manualmente. Alternativa a esperar OnMeasureStart.
    /// </summary>
    public void StartPattern()
    {
        if (pattern == null || pattern.Length == 0) return;
        currentEntryIndex = 0;
        nextAttackDspTime = AudioSettings.dspTime;
        isActive = true;
    }

    /// <summary>
    /// Detiene el patrón.
    /// </summary>
    public void StopPattern()
    {
        isActive = false;
    }

    private void OnMeasureStart()
    {
        if (!isActive)
        {
            StartPattern();
        }
    }

    private void OnResumed(double pauseDuration)
    {
        if (isActive)
        {
            nextAttackDspTime += pauseDuration;
        }
    }

    private void OnValidate()
    {
        if (pattern == null || pattern.Length == 0) return;

        float total = 0f;
        foreach (var entry in pattern)
            total += entry.BeatsConsumed;

        if (!Mathf.Approximately(total, 4f))
        {
            Debug.LogWarning(
                $"[EntityRhythmPattern] El patrón en '{gameObject.name}' suma {total} beats " +
                $"(se esperan 4 para un compás 4/4). Puede desalinearse del compás.",
                this);
        }
    }
}
