public enum SubdivisionType
{
    Quarter, // 1 beat
    Half,    // 2 beats
    Whole    // 4 beats
}

[System.Serializable]
public struct PatternEntry
{
    public SubdivisionType spacing;
    public bool isRest;
    public notesEnum note;

    public float BeatsConsumed =>
        spacing == SubdivisionType.Quarter ? 1f :
        spacing == SubdivisionType.Half    ? 2f : 4f;
}

public struct TimingResult
{
    public bool isOnBeat;
    public SubdivisionType subdivision;
    public double offsetFromBeat;
}
