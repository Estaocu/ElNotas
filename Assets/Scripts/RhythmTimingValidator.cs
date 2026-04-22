using UnityEngine;

public class RhythmTimingValidator : MonoBehaviour
{
    [SerializeField, Range(0.05f, 0.2f)] private float wholeQuarterWindow = 0.1f;
    [SerializeField, Range(0.05f, 0.2f)] private float halfWindow = 0.15f;

    public int GetMeterAward(double hitTime)
    {
        var rm = RhythmManager.Instance;
        if (rm == null) return 0;

        float offset = CalculateClosestBeatOffset(hitTime, rm, out int beatIndex);
        return EvaluateReward(beatIndex, offset);
    }

    private float CalculateClosestBeatOffset(double hitTime, RhythmManager rm, out int beatIndex)
    {
        double nextBeatTime = rm.NextSubBeatDspTime;
        double beatDuration = rm.SubBeatDuration;
        double prevBeatTime = nextBeatTime - beatDuration;

        float distToNext = Mathf.Abs((float)(hitTime - nextBeatTime));
        float distToPrev = Mathf.Abs((float)(hitTime - prevBeatTime));

        if (distToPrev <= distToNext)
        {
            beatIndex = EstimateBeatIndex(prevBeatTime, beatDuration);
            return distToPrev;
        }
        else
        {
            beatIndex = EstimateBeatIndex(nextBeatTime, beatDuration);
            return distToNext;
        }
    }

    private int EstimateBeatIndex(double beatTime, double beatDuration)
    {
        int idx = (int)((beatTime / beatDuration) % 16) + 1;
        return Mathf.Clamp(idx, 1, 16);
    }

    private int EvaluateReward(int beatIndex, float offset)
    {
        if (IsWholeBeat(beatIndex))
        {
            return offset <= wholeQuarterWindow ? 1 : 0;
        }

        if (IsHalfBeat(beatIndex))
        {
            return offset <= halfWindow ? 2 : 0;
        }

        // Quarter beat
        return offset <= wholeQuarterWindow ? 1 : 0;
    }

    private bool IsWholeBeat(int subBeat)
    {
        return ((subBeat - 1) % 4) == 0;
    }

    private bool IsHalfBeat(int subBeat)
    {
        return ((subBeat - 1) % 2) == 0;
    }
}
