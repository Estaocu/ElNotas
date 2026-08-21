using System;
using UnityEngine;

public enum RhythmTiming
{
    Early,
    Perfect,
    Late
}

[Serializable]
public struct RhythmQuantizationResult
{
    public RhythmPosition position;
    public double targetDspTime;
    public double inputDspTime;
    public double offset;
    public RhythmTiming timing;
    public bool isValid;

    public RhythmQuantizationResult(
        RhythmPosition position,
        double targetDspTime,
        double inputDspTime,
        double offset,
        RhythmTiming timing,
        bool isValid)
    {
        this.position = position;
        this.targetDspTime = targetDspTime;
        this.inputDspTime = inputDspTime;
        this.offset = offset;
        this.timing = timing;
        this.isValid = isValid;
    }
}

public class RhythmQuantizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmClock rhythmClock;

    [Header("Timing Windows")]
    [Tooltip("Maximum allowed time before a subbeat, in seconds.")]
    [SerializeField] private double negativeWindow = 0.15;

    [Tooltip("Maximum allowed time after a subbeat, in seconds.")]
    [SerializeField] private double positiveWindow = 0.075;

    public double NegativeWindow => negativeWindow;
    public double PositiveWindow => positiveWindow;

    public RhythmQuantizationResult Quantize(double inputDspTime)
    {
        if (rhythmClock == null || !rhythmClock.IsRunning)
            return CreateInvalidResult(inputDspTime);

        double elapsed = inputDspTime - rhythmClock.StartDspTime;

        if (elapsed < 0.0)
            return CreateInvalidResult(inputDspTime);

        double subBeatDuration = rhythmClock.SubBeatDuration;

        long lowerSubBeat = (long)Math.Floor(elapsed / subBeatDuration);
        long upperSubBeat = lowerSubBeat + 1;

        double lowerDspTime =
            GetDspTimeForSubBeat(lowerSubBeat);

        double upperDspTime =
            GetDspTimeForSubBeat(upperSubBeat);

        double distanceToLower =
            inputDspTime - lowerDspTime;

        double distanceToUpper =
            upperDspTime - inputDspTime;

        long targetSubBeat;

        if (distanceToLower <= distanceToUpper)
        {
            targetSubBeat = lowerSubBeat;
        }
        else
        {
            targetSubBeat = upperSubBeat;
        }

        double targetDspTime =
            GetDspTimeForSubBeat(targetSubBeat);

        double offset =
            inputDspTime - targetDspTime;

        RhythmPosition position =
            GetPositionForSubBeat(targetSubBeat);

        bool isValid =
            offset < 0.0
                ? Mathf.Abs((float)offset) <= negativeWindow
                : offset <= positiveWindow;

        RhythmTiming timing;

        if (offset < 0.0)
            timing = RhythmTiming.Early;
        else if (offset > 0.0)
            timing = RhythmTiming.Late;
        else
            timing = RhythmTiming.Perfect;

        return new RhythmQuantizationResult(
            position,
            targetDspTime,
            inputDspTime,
            offset,
            timing,
            isValid
        );
    }

    public RhythmQuantizationResult QuantizeCurrentTime()
    {
        return Quantize(AudioSettings.dspTime);
    }

    private double GetDspTimeForSubBeat(long totalSubBeat)
    {
        int bar = (int)(totalSubBeat / RhythmClock.SubBeatsPerBar);
        int subBeat = (int)(totalSubBeat % RhythmClock.SubBeatsPerBar);

        if (subBeat < 0)
        {
            subBeat += RhythmClock.SubBeatsPerBar;
            bar--;
        }

        RhythmPosition position = new RhythmPosition(
            bar,
            subBeat / RhythmClock.SubBeatsPerBeat,
            subBeat
        );

        return rhythmClock.GetDspTimeForPosition(position);
    }

    private RhythmPosition GetPositionForSubBeat(long totalSubBeat)
    {
        int bar = (int)(totalSubBeat / RhythmClock.SubBeatsPerBar);
        int subBeat = (int)(totalSubBeat % RhythmClock.SubBeatsPerBar);

        if (subBeat < 0)
        {
            subBeat += RhythmClock.SubBeatsPerBar;
            bar--;
        }

        int beat =
            subBeat / RhythmClock.SubBeatsPerBeat;

        return new RhythmPosition(
            bar,
            beat,
            subBeat
        );
    }

    private RhythmQuantizationResult CreateInvalidResult(
        double inputDspTime)
    {
        return new RhythmQuantizationResult(
            new RhythmPosition(0, 0, 0),
            0.0,
            inputDspTime,
            0.0,
            RhythmTiming.Perfect,
            false
        );
    }
}