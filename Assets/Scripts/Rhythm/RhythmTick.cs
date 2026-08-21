using System;

[Serializable]
public struct RhythmTick
{
    public RhythmPosition position;
    public double dspTime;

    public RhythmTick(RhythmPosition position, double dspTime)
    {
        this.position = position;
        this.dspTime = dspTime;
    }
}