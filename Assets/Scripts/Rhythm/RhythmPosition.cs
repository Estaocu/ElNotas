using System;

[Serializable]
public struct RhythmPosition
{
    public int bar;
    public int beat;
    public int subBeat;

    public RhythmPosition(int bar, int beat, int subBeat)
    {
        this.bar = bar;
        this.beat = beat;
        this.subBeat = subBeat;
    }

    public override string ToString()
    {
        return $"Bar {bar}, Beat {beat}, SubBeat {subBeat}";
    }
}