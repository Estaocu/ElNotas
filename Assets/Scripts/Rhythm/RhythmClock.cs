using System;
using System.Collections;
using UnityEngine;

public class RhythmClock : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private double bpm = 72.0;

    public const int BeatsPerBar = 4;
    public const int SubBeatsPerBeat = 2;
    public const int SubBeatsPerBar = 8;

    private double startDspTime;
    private double pauseStartDspTime;

    private bool isRunning;
    private bool isPaused;

    private long lastProcessedSubBeat = -1;

    // Current tempo segment.
    // This allows the BPM to change without resetting absolute musical time.
    private double tempoSegmentStartDspTime;
    private long tempoSegmentStartSubBeat;

    // Pending BPM change.
    private bool hasPendingBpmChange;
    private double pendingBpm;
    private double pendingBpmDspTime;
    private long pendingBpmSubBeat;

    public static event Action<RhythmTick> OnSubBeat;
    public static event Action<RhythmTick> OnBeat;
    public static event Action<RhythmTick> OnBar;

    public static event Action OnPaused;
    public static event Action OnResumed;

    // Current BPM.
    public double CurrentBpm => bpm;

    // Duration of one quarter note in seconds.
    public double BeatDuration => 60.0 / bpm;

    // Duration of one eighth note in seconds.
    public double SubBeatDuration => BeatDuration / SubBeatsPerBeat;

    // Duration of one complete bar in seconds.
    public double BarDuration => BeatDuration * BeatsPerBar;

    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;

    public double StartDspTime => startDspTime;

    // Absolute musical subbeat currently occupied by the clock.
    public long CurrentAbsoluteSubBeat
    {
        get
        {
            if (!isRunning)
                return -1;

            double currentDspTime = isPaused
                ? pauseStartDspTime
                : AudioSettings.dspTime;

            if (currentDspTime < tempoSegmentStartDspTime)
                return tempoSegmentStartSubBeat;

            double elapsed =
                currentDspTime - tempoSegmentStartDspTime;

            long subBeats =
                (long)(elapsed / SubBeatDuration);

            return tempoSegmentStartSubBeat + subBeats;
        }
    }

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

        tempoSegmentStartDspTime = dspStartTime;
        tempoSegmentStartSubBeat = 0;

        lastProcessedSubBeat = -1;

        hasPendingBpmChange = false;

        isPaused = false;
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
        isPaused = false;

        lastProcessedSubBeat = -1;

        hasPendingBpmChange = false;
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
        tempoSegmentStartDspTime += pausedDuration;

        if (hasPendingBpmChange)
            pendingBpmDspTime += pausedDuration;

        isPaused = false;

        OnResumed?.Invoke();
    }

    // Schedules a BPM change at an exact DSP timestamp.
    // The timestamp should correspond to a bar boundary.
    public void ScheduleBpmChange(
        double newBpm,
        double dspTime)
    {
        if (newBpm <= 0.0)
        {
            Debug.LogError(
                "RhythmClock requires a BPM greater than zero.");

            return;
        }

        if (!isRunning)
        {
            bpm = newBpm;
            return;
        }

        long currentSubBeat = CurrentAbsoluteSubBeat;

        if (currentSubBeat < 0)
            return;

        // Find the musical subbeat represented by the requested DSP time.
        long targetSubBeat =
            GetAbsoluteSubBeatAtDspTime(dspTime);

        // BPM changes are only allowed at bar boundaries.
        long targetBarStart =
            (targetSubBeat / SubBeatsPerBar) * SubBeatsPerBar;

        if (targetSubBeat != targetBarStart)
        {
            targetBarStart += SubBeatsPerBar;

            dspTime =
                GetDspTimeForAbsoluteSubBeat(targetBarStart);
        }

        pendingBpm = newBpm;
        pendingBpmDspTime = dspTime;
        pendingBpmSubBeat = targetBarStart;
        hasPendingBpmChange = true;
    }

    // Schedules a BPM change at the beginning of the next bar.
    public void ScheduleBpmChangeAtNextBar(double newBpm)
    {
        if (!isRunning)
        {
            bpm = newBpm;
            return;
        }

        double nextBarDspTime = NextBarDspTime;

        ScheduleBpmChange(
            newBpm,
            nextBarDspTime);
    }

    // Cancels a pending BPM change.
    public void CancelPendingBpmChange()
    {
        hasPendingBpmChange = false;
    }

    // Waits for the specified number of future musical subbeats.
    // The wait follows the RhythmClock position and therefore pauses with the clock.
    public IEnumerator WaitForSubBeats(int subBeats)
    {
        if (subBeats <= 0)
            yield break;

        if (!isRunning)
            yield break;

        long startSubBeat = CurrentAbsoluteSubBeat;

        if (startSubBeat < 0)
            yield break;

        long targetSubBeat =
            startSubBeat + subBeats;

        while (isRunning &&
               CurrentAbsoluteSubBeat < targetSubBeat)
        {
            yield return null;
        }
    }

    // Converts a DSP timestamp into a musical position.
    public RhythmPosition GetPositionAtDspTime(double dspTime)
    {
        if (!isRunning)
            return new RhythmPosition(0, 0, 0);

        double referenceDspTime =
            isPaused
                ? pauseStartDspTime
                : dspTime;

        long absoluteSubBeat =
            GetAbsoluteSubBeatAtDspTime(referenceDspTime);

        if (absoluteSubBeat < 0)
            absoluteSubBeat = 0;

        int bar =
            (int)(absoluteSubBeat / SubBeatsPerBar);

        int subBeat =
            (int)(absoluteSubBeat % SubBeatsPerBar);

        int beat =
            subBeat / SubBeatsPerBeat;

        return new RhythmPosition(
            bar,
            beat,
            subBeat);
    }

    // Converts a musical position into its DSP timestamp.
    public double GetDspTimeForPosition(
        RhythmPosition position)
    {
        long totalSubBeats =
            (long)position.bar * SubBeatsPerBar +
            position.subBeat;

        return GetDspTimeForAbsoluteSubBeat(totalSubBeats);
    }

    public double NextSubBeatDspTime
    {
        get
        {
            long currentSubBeat =
                CurrentAbsoluteSubBeat;

            if (currentSubBeat < 0)
                return startDspTime;

            return GetDspTimeForAbsoluteSubBeat(
                currentSubBeat + 1);
        }
    }

    public double NextBeatDspTime
    {
        get
        {
            long currentSubBeat =
                CurrentAbsoluteSubBeat;

            if (currentSubBeat < 0)
                return startDspTime;

            long currentBeat =
                currentSubBeat / SubBeatsPerBeat;

            long nextBeatSubBeat =
                (currentBeat + 1) * SubBeatsPerBeat;

            return GetDspTimeForAbsoluteSubBeat(
                nextBeatSubBeat);
        }
    }

    public double NextBarDspTime
    {
        get
        {
            long currentSubBeat =
                CurrentAbsoluteSubBeat;

            if (currentSubBeat < 0)
                return startDspTime;

            long currentBar =
                currentSubBeat / SubBeatsPerBar;

            long nextBarSubBeat =
                (currentBar + 1) * SubBeatsPerBar;

            return GetDspTimeForAbsoluteSubBeat(
                nextBarSubBeat);
        }
    }

    private void ProcessPendingTicks()
    {
        double currentDspTime =
            AudioSettings.dspTime;

        long currentSubBeat =
            CurrentAbsoluteSubBeat;

        if (currentSubBeat < 0)
            return;

        while (lastProcessedSubBeat < currentSubBeat)
        {
            long nextSubBeat =
                lastProcessedSubBeat + 1;

            // Apply a pending tempo change exactly at the
            // musical boundary before creating that tick.
            if (hasPendingBpmChange &&
                nextSubBeat >= pendingBpmSubBeat)
            {
                ApplyPendingBpmChange();
            }

            lastProcessedSubBeat++;

            RhythmTick tick =
                CreateTick(lastProcessedSubBeat);

            OnSubBeat?.Invoke(tick);

            if (tick.position.subBeat % SubBeatsPerBeat == 0)
                OnBeat?.Invoke(tick);

            if (tick.position.subBeat == 0)
                OnBar?.Invoke(tick);

            currentSubBeat =
                CurrentAbsoluteSubBeat;

            if (currentDspTime < tick.dspTime)
                break;
        }
    }

    private void ApplyPendingBpmChange()
    {
        bpm = pendingBpm;

        tempoSegmentStartDspTime =
            pendingBpmDspTime;

        tempoSegmentStartSubBeat =
            pendingBpmSubBeat;

        hasPendingBpmChange = false;
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
                subBeat);

        double dspTime =
            GetDspTimeForAbsoluteSubBeat(
                totalSubBeat);

        return new RhythmTick(
            position,
            dspTime);
    }

    private long GetAbsoluteSubBeatAtDspTime(
        double dspTime)
    {
        if (!isRunning)
            return -1;

        if (dspTime < tempoSegmentStartDspTime)
            return tempoSegmentStartSubBeat;

        double elapsed =
            dspTime - tempoSegmentStartDspTime;

        long subBeats =
            (long)(elapsed / SubBeatDuration);

        return tempoSegmentStartSubBeat + subBeats;
    }

    private double GetDspTimeForAbsoluteSubBeat(
        long absoluteSubBeat)
    {
        if (absoluteSubBeat <= tempoSegmentStartSubBeat)
        {
            return tempoSegmentStartDspTime;
        }

        long relativeSubBeat =
            absoluteSubBeat - tempoSegmentStartSubBeat;

        return tempoSegmentStartDspTime +
               relativeSubBeat * SubBeatDuration;
    }
}