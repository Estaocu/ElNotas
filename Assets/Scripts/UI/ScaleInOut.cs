using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Animates the local scale of any object (UI included) from 0 to its original scale and back.
/// Scales from the object's pivot. Call scaleIn() / scaleOut() from any other script or UnityEvent.
/// Each instance has its own curve and duration.
/// </summary>
public class ScaleInOut : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Maps normalized progress (0..1) to scale multiplier. Scale out plays it in reverse.")]
    [SerializeField] private AnimationCurve scaleCurve = createExponentialEaseOutCurve();

    [Tooltip("Seconds a full 0 -> 1 transition takes.")]
    [SerializeField, Min(0f)] private float duration = 0.35f;

    [Tooltip("Ignore Time.timeScale (recommended for UI, so it still animates while the game is paused).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Behaviour")]
    [Tooltip("Start with scale 0 on Awake.")]
    [SerializeField] private bool startHidden = false;

    [Tooltip("Play scale in automatically every time the object is enabled.")]
    [SerializeField] private bool playOnEnable = false;

    [Tooltip("Deactivate the GameObject when the scale out finishes.")]
    [SerializeField] private bool deactivateOnScaleOut = false;

    [Header("Events")]
    public UnityEvent onScaleInFinished;
    public UnityEvent onScaleOutFinished;

    private Vector3 fullScale = Vector3.one;
    private float progress = 1f; // 0 = hidden, 1 = fully visible
    private bool initialized;
    private Coroutine currentRoutine;
    private Action pendingCallback;

    // ---------- Public API ----------

    /// <summary>True while a transition is running.</summary>
    public bool isAnimating { get; private set; }

    /// <summary>True if the object is visible or heading to visible.</summary>
    public bool isVisible { get; private set; } = true;

    public float animationDuration
    {
        get => duration;
        set => duration = Mathf.Max(0f, value);
    }

    public AnimationCurve animationCurve
    {
        get => scaleCurve;
        set => scaleCurve = value ?? createExponentialEaseOutCurve();
    }

    /// <summary>Zoom in from scale 0 to full scale. Activates the GameObject if needed.</summary>
    public void scaleIn(Action onComplete = null)
    {
        ensureInitialized();
        isVisible = true;

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            // A parent is inactive: coroutines can't run. Apply the final state directly.
            Debug.LogWarning($"[ScaleInOut] '{name}' has an inactive parent, applying scale instantly.", this);
            setProgress(1f);
            onScaleInFinished?.Invoke();
            onComplete?.Invoke();
            return;
        }

        startTransition(1f, onComplete);
    }

    /// <summary>Zoom out from the current scale to 0.</summary>
    public void scaleOut(Action onComplete = null)
    {
        ensureInitialized();
        isVisible = false;

        if (!gameObject.activeInHierarchy)
        {
            // Nothing to animate if it isn't even active.
            setProgress(0f);
            onScaleOutFinished?.Invoke();
            onComplete?.Invoke();
            return;
        }

        startTransition(0f, onComplete);
    }

    /// <summary>Skips the animation and snaps to visible / hidden.</summary>
    public void setVisibleInstant(bool visible)
    {
        ensureInitialized();
        stopCurrentRoutine();
        isVisible = visible;
        setProgress(visible ? 1f : 0f);
    }

    // ---------- Unity messages ----------

    private void Awake()
    {
        ensureInitialized();
    }

    private void OnEnable()
    {
        ensureInitialized();

        if (playOnEnable)
        {
            setProgress(0f);
            scaleIn();
        }
    }

    private void OnDisable()
    {
        // Coroutines die when the object is disabled: snap to the target so we never stay half scaled.
        if (isAnimating)
        {
            stopCurrentRoutine();
            setProgress(isVisible ? 1f : 0f);
        }
    }

    // ---------- Internals ----------

    private void ensureInitialized()
    {
        if (initialized) return;
        initialized = true;

        fullScale = transform.localScale;

        // If the object was left at scale 0 in the editor, "full scale" would be 0 and it would never show up.
        if (fullScale.sqrMagnitude < 0.0001f) fullScale = Vector3.one;

        if (startHidden)
        {
            isVisible = false;
            setProgress(0f);
        }
        else
        {
            setProgress(1f);
        }
    }

    private void startTransition(float target, Action onComplete)
    {
        stopCurrentRoutine();
        pendingCallback = onComplete;
        isAnimating = true;
        currentRoutine = StartCoroutine(transitionRoutine(target));
    }

    private IEnumerator transitionRoutine(float target)
    {
        float startProgress = progress;
        // Scale the time by the remaining distance so interrupted transitions keep a constant speed.
        float totalTime = duration * Mathf.Abs(target - startProgress);
        float elapsed = 0f;

        while (elapsed < totalTime)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);
            setProgress(Mathf.Lerp(startProgress, target, t));
            yield return null;
        }

        setProgress(target);
        isAnimating = false;
        currentRoutine = null;

        Action callback = pendingCallback;
        pendingCallback = null;

        if (target >= 1f) onScaleInFinished?.Invoke();
        else onScaleOutFinished?.Invoke();

        callback?.Invoke();

        if (target <= 0f && deactivateOnScaleOut) gameObject.SetActive(false);
    }

    private void stopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        isAnimating = false;
        pendingCallback = null;
    }

    private void setProgress(float newProgress)
    {
        progress = newProgress;
        float multiplier = Mathf.Max(0f, scaleCurve.Evaluate(progress));
        transform.localScale = fullScale * multiplier;
    }

    /// <summary>
    /// 1 - e^(-k t), normalized so it ends exactly at 1. Fast start, soft landing.
    /// Tangents are analytic so the curve stays smooth with few keys.
    /// </summary>
    private static AnimationCurve createExponentialEaseOutCurve()
    {
        const int samples = 12;
        const float k = 5f;
        float norm = 1f - Mathf.Exp(-k);

        Keyframe[] keys = new Keyframe[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            float value = (1f - Mathf.Exp(-k * t)) / norm;
            float tangent = k * Mathf.Exp(-k * t) / norm;
            keys[i] = new Keyframe(t, value, tangent, tangent);
        }

        return new AnimationCurve(keys);
    }
}