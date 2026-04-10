using System;
using UnityEngine;

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Tempo")]
    [SerializeField] private int bpm = 120;

    [Header("Timing Window")]
    [Tooltip("Fracción del beatDuration como media-ventana. 0.2 = ±20% del beat.")]
    [SerializeField] [Range(0.05f, 0.5f)] private float timingWindowPercent = 0.2f;

    // --- Estado interno ---
    private double beatDuration;
    private int currentBeat;
    private double musicStartDspTime;
    private double pauseStartDspTime;
    private double totalPausedDuration;
    private bool isPlaying;
    private bool isPaused;

    // --- Propiedades públicas ---
    public int CurrentBeat => currentBeat;
    public double BeatDuration => beatDuration;
    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public int BPM => bpm;

    // --- Eventos ---
    public event Action<int> OnBeat;
    public event Action OnMeasureStart;
    public event Action<double> OnResumed; // pasa duración de la pausa

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RecalculateBeatDuration();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        double elapsed = GetElapsedMusicalTime();
        int totalBeatsElapsed = (int)(elapsed / beatDuration);
        int expectedBeat = (totalBeatsElapsed % 4) + 1;

        if (expectedBeat != currentBeat)
        {
            currentBeat = expectedBeat;
            OnBeat?.Invoke(currentBeat);
            if (currentBeat == 1) OnMeasureStart?.Invoke();
        }
    }

    // ==================== API Pública ====================

    public void StartMusic()
    {
        if (musicSource == null)
        {
            Debug.LogError("[RhythmManager] No hay AudioSource asignado.");
            return;
        }

        RecalculateBeatDuration();
        musicSource.Play();
        musicStartDspTime = AudioSettings.dspTime;
        totalPausedDuration = 0;
        currentBeat = 1;
        isPlaying = true;
        isPaused = false;

        OnBeat?.Invoke(1);
        OnMeasureStart?.Invoke();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
        isPlaying = false;
        isPaused = false;
    }

    public void Pause()
    {
        if (!isPlaying || isPaused) return;
        isPaused = true;
        pauseStartDspTime = AudioSettings.dspTime;
        if (musicSource != null) musicSource.Pause();
    }

    public void Resume()
    {
        if (!isPlaying || !isPaused) return;
        double pauseDuration = AudioSettings.dspTime - pauseStartDspTime;
        totalPausedDuration += pauseDuration;
        isPaused = false;
        if (musicSource != null) musicSource.UnPause();
        OnResumed?.Invoke(pauseDuration);
    }

    public void SetBPM(int newBpm)
    {
        bpm = Mathf.Max(1, newBpm);
        RecalculateBeatDuration();
    }

    /// <summary>
    /// Evalúa si un hit (en dspTime) cae dentro de la ventana de un tipo de subdivisión.
    /// </summary>
    public TimingResult EvaluateHit(double hitDspTime, SubdivisionType subdivision)
    {
        var result = new TimingResult
        {
            isOnBeat = false,
            subdivision = subdivision,
            offsetFromBeat = double.MaxValue
        };

        if (!isPlaying) return result;

        double elapsed = hitDspTime - musicStartDspTime - totalPausedDuration;
        if (elapsed < 0) return result;

        double subdivisionInterval = SubdivisionToBeats(subdivision) * beatDuration;
        double positionInGrid = elapsed % subdivisionInterval;

        // Distancia al beat más cercano de esta subdivisión (puede ser antes o después)
        double offset = positionInGrid;
        if (offset > subdivisionInterval * 0.5)
            offset -= subdivisionInterval;

        double halfWindow = beatDuration * timingWindowPercent;
        result.offsetFromBeat = offset;
        result.isOnBeat = Math.Abs(offset) <= halfWindow;

        return result;
    }

    /// <summary>
    /// Devuelve el tiempo musical transcurrido corregido por pausas.
    /// </summary>
    public double GetElapsedMusicalTime()
    {
        double pauseCorrection = isPaused
            ? totalPausedDuration + (AudioSettings.dspTime - pauseStartDspTime)
            : totalPausedDuration;

        return AudioSettings.dspTime - musicStartDspTime - pauseCorrection;
    }

    /// <summary>
    /// Devuelve el dspTime absoluto del próximo beat de la subdivisión dada.
    /// </summary>
    public double GetNextBeatDspTime(SubdivisionType subdivision)
    {
        double elapsed = GetElapsedMusicalTime();
        double interval = SubdivisionToBeats(subdivision) * beatDuration;
        double beatsPassed = Math.Floor(elapsed / interval);
        double nextBeatElapsed = (beatsPassed + 1) * interval;
        double pauseCorrection = isPaused
            ? totalPausedDuration + (AudioSettings.dspTime - pauseStartDspTime)
            : totalPausedDuration;

        return musicStartDspTime + pauseCorrection + nextBeatElapsed;
    }

    // ==================== Helpers ====================

    private void RecalculateBeatDuration()
    {
        beatDuration = 60.0 / Mathf.Max(1, bpm);
    }

    private static double SubdivisionToBeats(SubdivisionType type)
    {
        return type switch
        {
            SubdivisionType.Quarter => 1.0,
            SubdivisionType.Half    => 2.0,
            SubdivisionType.Whole   => 4.0,
            _ => 1.0
        };
    }
}
