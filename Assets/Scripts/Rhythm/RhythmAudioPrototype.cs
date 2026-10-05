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
    private AudioSource secondaryAudioSource;

    private AudioClip currentClip;
    private AudioClip pendingClip;

    private bool isPrimarySourceActive = true;
    private bool hasPendingTrackChange;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        secondaryAudioSource = gameObject.AddComponent<AudioSource>();
        secondaryAudioSource.playOnAwake = false;
        secondaryAudioSource.loop = true;

        CopyAudioSourceSettings(audioSource, secondaryAudioSource);
    }

    private void OnEnable()
    {
        RhythmClock.OnBar += HandleBar;
        RhythmClock.OnPaused += HandleClockPaused;
        RhythmClock.OnResumed += HandleClockResumed;
    }

    private void OnDisable()
    {
        RhythmClock.OnBar -= HandleBar;
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

        currentClip = loopClip;
        audioSource.clip = currentClip;

        rhythmClock.StartAt(startDspTime);

        audioSource.PlayScheduled(startDspTime);
    }

    private void HandleBar(RhythmTick tick)
    {
        if (!hasPendingTrackChange || pendingClip == null)
        {
            return;
        }

        ScheduleTrackChange(pendingClip, tick.dspTime);

        pendingClip = null;
        hasPendingTrackChange = false;
    }

    private void ScheduleTrackChange(AudioClip newClip, double barDspTime)
    {
        AudioSource nextSource = GetInactiveSource();
        AudioSource currentSource = GetActiveSource();

        nextSource.clip = newClip;
        nextSource.loop = true;

        nextSource.PlayScheduled(barDspTime);

        currentSource.SetScheduledEndTime(barDspTime);

        currentClip = newClip;
        isPrimarySourceActive = !isPrimarySourceActive;
    }

    private AudioSource GetActiveSource()
    {
        return isPrimarySourceActive
            ? audioSource
            : secondaryAudioSource;
    }

    private AudioSource GetInactiveSource()
    {
        return isPrimarySourceActive
            ? secondaryAudioSource
            : audioSource;
    }

    private void HandleClockPaused()
    {
        audioSource.Pause();
        secondaryAudioSource.Pause();
    }

    private void HandleClockResumed()
    {
        audioSource.UnPause();
        secondaryAudioSource.UnPause();
    }

    private void CopyAudioSourceSettings(
        AudioSource source,
        AudioSource destination)
    {
        destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
        destination.mute = source.mute;
        destination.bypassEffects = source.bypassEffects;
        destination.bypassListenerEffects = source.bypassListenerEffects;
        destination.priority = source.priority;
        destination.volume = source.volume;
        destination.pitch = source.pitch;
        destination.panStereo = source.panStereo;
        destination.spatialBlend = source.spatialBlend;
        destination.reverbZoneMix = source.reverbZoneMix;
        destination.dopplerLevel = source.dopplerLevel;
        destination.spread = source.spread;
        destination.rolloffMode = source.rolloffMode;
        destination.minDistance = source.minDistance;
        destination.maxDistance = source.maxDistance;
    }

    public void ChangeTrack(AudioClip newClip)
    {
        if (newClip == null)
        {
            return;
        }

        if (newClip == currentClip && !hasPendingTrackChange)
        {
            return;
        }

        pendingClip = newClip;
        hasPendingTrackChange = true;
    }

    public void Stop()
    {
        audioSource.Stop();
        secondaryAudioSource.Stop();

        pendingClip = null;
        currentClip = null;
        hasPendingTrackChange = false;

        rhythmClock.Stop();
    }
}