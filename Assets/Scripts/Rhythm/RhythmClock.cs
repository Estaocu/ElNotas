using System;
using UnityEngine;

public class RhythmClock : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private double bpm = 96.0;

    public const int BeatsPerBar = 4;
    public const int SubBeatsPerBeat = 2;
    public const int SubBeatsPerBar = 8;

    private double startDspTime;
    private double nextBeatTime;
    private double pauseStartDspTime;

    private bool isRunning;
    private bool isPaused;

    private long lastProcessedSubBeat = -1;

    public static event Action<RhythmTick> OnSubBeat;
    public static event Action<RhythmTick> OnBeat;
    public static event Action<RhythmTick> OnBar;

    public static event Action OnPaused;
    public static event Action OnResumed;

    // Duration of one quarter note in seconds.
    public double BeatDuration => 60.0 / bpm;

    // Duration of one eighth note in seconds.
    public double SubBeatDuration => BeatDuration / SubBeatsPerBeat;

    // Duration of one complete bar in seconds.
    public double BarDuration => BeatDuration * BeatsPerBar;

    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;

    public double StartDspTime => startDspTime;

    // DSP time elapsed since the musical clock started.
    public double ElapsedDspTime
    {
        get
        {
            if (!isRunning)
                return 0.0;

            double currentDspTime = isPaused
                ? pauseStartDspTime
                : AudioSettings.dspTime;

            return currentDspTime - startDspTime;
        }
    }

    public RhythmPosition CurrentPosition =>
        GetPositionAtDspTime(AudioSettings.dspTime);

    public int CurrentBar => CurrentPosition.bar;

    public int CurrentBeat => CurrentPosition.beat;

    public int CurrentSubBeat => CurrentPosition.subBeat;

    private void Update()
    {
        if (!isRunning || isPaused)
            return;

        ProcessPendingTicks();
    }

    // Starts the clock at the specified DSP time.
    // This timestamp represents the beginning of bar 0, beat 0, subbeat 0.
    public void StartAt(double dspStartTime)
    {
        startDspTime = dspStartTime;
        nextBeatTime = dspStartTime;
        lastProcessedSubBeat = -1;

        isPaused = false;
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
        isPaused = false;
        lastProcessedSubBeat = -1;
    }

    public void Pause()
    {
        if (!isRunning || isPaused)
            return;

        isPaused = true;
        pauseStartDspTime = AudioSettings.dspTime;

        OnPaused?.Invoke();
    }

    public void Resume()
    {
        if (!isRunning || !isPaused)
            return;

        double pausedDuration =
            AudioSettings.dspTime - pauseStartDspTime;

        startDspTime += pausedDuration;
        nextBeatTime += pausedDuration;

        isPaused = false;

        OnResumed?.Invoke();
    }

    // Converts a DSP timestamp into a musical position.
    public RhythmPosition GetPositionAtDspTime(double dspTime)
    {
        if (!isRunning)
            return new RhythmPosition(0, 0, 0);

        double referenceDspTime = isPaused
            ? pauseStartDspTime
            : dspTime;

        double elapsed = referenceDspTime - startDspTime;

        if (elapsed < 0.0)
            return new RhythmPosition(0, 0, 0);

        long totalSubBeats =
            (long)(elapsed / SubBeatDuration);

        int bar =
            (int)(totalSubBeats / SubBeatsPerBar);

        int subBeat =
            (int)(totalSubBeats % SubBeatsPerBar);

        int beat =
            subBeat / SubBeatsPerBeat;

        return new RhythmPosition(
            bar,
            beat,
            subBeat
        );
    }

    // Converts a musical position into its exact DSP timestamp.
    public double GetDspTimeForPosition(
        RhythmPosition position)
    {
        long totalSubBeats =
            (long)position.bar * SubBeatsPerBar +
            position.subBeat;

        return startDspTime +
               totalSubBeats * SubBeatDuration;
    }

    public double NextSubBeatDspTime
    {
        get
        {
            double elapsed =
                ElapsedDspTime;

            if (elapsed < 0.0)
                return startDspTime;

            long currentSubBeat =
                (long)(elapsed / SubBeatDuration);

            return startDspTime +
                   (currentSubBeat + 1) * SubBeatDuration;
        }
    }

    public double NextBeatDspTime
    {
        get
        {
            double elapsed =
                ElapsedDspTime;

            if (elapsed < 0.0)
                return startDspTime;

            long currentBeat =
                (long)(elapsed / BeatDuration);

            return startDspTime +
                   (currentBeat + 1) * BeatDuration;
        }
    }

    public double NextBarDspTime
    {
        get
        {
            double elapsed =
                ElapsedDspTime;

            if (elapsed < 0.0)
                return startDspTime;

            long currentBar =
                (long)(elapsed / BarDuration);

            return startDspTime +
                   (currentBar + 1) * BarDuration;
        }
    }

    private void ProcessPendingTicks()
    {
        double elapsed =
            AudioSettings.dspTime - startDspTime;

        if (elapsed < 0.0)
            return;

        long currentSubBeat =
            (long)(elapsed / SubBeatDuration);

        while (lastProcessedSubBeat < currentSubBeat)
        {
            lastProcessedSubBeat++;

            RhythmTick tick =
                CreateTick(lastProcessedSubBeat);

            OnSubBeat?.Invoke(tick);

            if (tick.position.subBeat % SubBeatsPerBeat == 0)
                OnBeat?.Invoke(tick);

            if (tick.position.subBeat == 0)
                OnBar?.Invoke(tick);
        }
    }

    private RhythmTick CreateTick(long totalSubBeat)
    {
        int bar =
            (int)(totalSubBeat / SubBeatsPerBar);

        int subBeat =
            (int)(totalSubBeat % SubBeatsPerBar);

        int beat =
            subBeat / SubBeatsPerBeat;

        RhythmPosition position =
            new RhythmPosition(
                bar,
                beat,
                subBeat
            );

        double dspTime =
            startDspTime +
            totalSubBeat * SubBeatDuration;

        return new RhythmTick(
            position,
            dspTime
        );
    }
}