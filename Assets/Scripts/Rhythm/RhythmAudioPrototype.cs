using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RhythmAudioPrototype : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmClock rhythmClock;

    [Header("Audio")]
    [SerializeField] private AudioClip loopClip;

    [Header("Startup")]
    [SerializeField] private double startDelay = 1.0;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    private void OnEnable()
    {
        RhythmClock.OnPaused += HandleClockPaused;
        RhythmClock.OnResumed += HandleClockResumed;
    }

    private void OnDisable()
    {
        RhythmClock.OnPaused -= HandleClockPaused;
        RhythmClock.OnResumed -= HandleClockResumed;
    }

    private void Start()
    {
        StartRhythm();
    }

    private void StartRhythm()
    {
        if (rhythmClock == null)
        {
            Debug.LogError("RhythmAudioPrototype requires a RhythmClock reference.");
            return;
        }

        if (loopClip == null)
        {
            Debug.LogError("RhythmAudioPrototype requires an AudioClip.");
            return;
        }

        double startDspTime = AudioSettings.dspTime + startDelay;

        audioSource.clip = loopClip;

        rhythmClock.StartAt(startDspTime);

        audioSource.PlayScheduled(startDspTime);
    }

    private void HandleClockPaused()
    {
        audioSource.Pause();
    }

    private void HandleClockResumed()
    {
        audioSource.UnPause();
    }

    public void Stop()
    {
        audioSource.Stop();
        rhythmClock.Stop();
    }
}