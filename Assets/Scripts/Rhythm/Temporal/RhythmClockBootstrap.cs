using UnityEngine;

public class RhythmClockBootstrap : MonoBehaviour
{
    [SerializeField] private RhythmClock rhythmClock;

    private void Start()
{
    double startTime = AudioSettings.dspTime + 1.0;

    Debug.Log($"Starting RhythmClock at DSP {startTime:F4}");

    rhythmClock.StartAt(startTime);
}

    
}