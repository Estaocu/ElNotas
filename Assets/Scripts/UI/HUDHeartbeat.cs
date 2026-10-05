using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class HUDHeartbeat : MonoBehaviour
{
    [SerializeField] private float pulseDuration = 0.25f;   // seconds, real time
    [SerializeField] private float amplitude = 0.12f;       // 0.12 = +12% scale at curve peak
    [SerializeField] private bool useUnscaledTime = false;   // keeps pulsing if timeScale = 0
    [SerializeField] private AnimationCurve pulseCurve;     // X: 0..1 normalized pulse, Y: 0..1 intensity

    [SerializeField] private bool eights = true;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Coroutine pulseCoroutine;

    private RhythmClock clock;

    private void Reset()
    {
        // Fast attack, slower release
        pulseCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.0f),
            new Keyframe(0.20f, 1.0f),
            new Keyframe(1.00f, 0.0f));
    }

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        clock = FindFirstObjectByType<RhythmClock>();
    }

    private void OnEnable()
    {
        baseScale = rectTransform.localScale;
        if(eights)
        {
            RhythmClock.OnSubBeat += ExecuteBeat;
        }

        if (!eights)
        {
            RhythmClock.OnBeat += ExecuteBeat;
        }
        
    }

    private void OnDisable()
    {
        if(eights)
        {
            RhythmClock.OnSubBeat -= ExecuteBeat;
        }

        if (!eights)
        {
            RhythmClock.OnBeat -= ExecuteBeat;
        }
        // Coroutines stop automatically on disable, so restore scale manually
        pulseCoroutine = null;
        rectTransform.localScale = baseScale;
    }

    private void ExecuteBeat(RhythmTick tick)
    {
        // Restart from the beginning if the previous pulse hasn't finished
        StartCoroutine(HoldOrderRoutine());
    }

    private IEnumerator pulseRoutine()
    {
        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            float normalized = elapsed / pulseDuration;
            rectTransform.localScale = baseScale * (1f + pulseCurve.Evaluate(normalized) * amplitude);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = baseScale;
        pulseCoroutine = null;
    }
    public static float ToSingle(double value)
    {
        return (float)value;
    }

    private IEnumerator HoldOrderRoutine()
    {
        float time = ToSingle(clock.SubBeatDuration);

        yield return new WaitForSeconds(2*time*0.95f);

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(pulseRoutine());

    }
}