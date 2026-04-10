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
            singer.SpawnSoundwave(melody, entry.note);
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
            if (looping)
                currentEntryIndex = 0;
            else
                isActive = false;
        }
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
