using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Animates the local scale of any object (UI included) between 0 and its original scale, from its pivot.
/// Orders issued while a transition is playing are queued and executed in order.
/// </summary>
public class ScaleInOut : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Maps normalized progress (0..1) to scale multiplier. Scale out plays it in reverse.")]
    [SerializeField] private AnimationCurve scaleCurve = createExponentialEaseOutCurve();
    [Tooltip("Seconds a full 0 -> 1 transition takes.")]
    [SerializeField, Min(0f)] private float duration = 0.35f;
    [Tooltip("Ignore Time.timeScale (so UI still animates while the game is paused).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Behaviour")]
    [SerializeField] private bool startHidden = false;
    [Tooltip("Play scale in every time the object is enabled.")]
    [SerializeField] private bool playOnEnable = false;
    [Tooltip("Deactivate the GameObject when the last queued scale out finishes.")]
    [SerializeField] private bool deactivateOnScaleOut = false;

    [Header("Events")]
    public UnityEvent onScaleInFinished;
    public UnityEvent onScaleOutFinished;

    private readonly Queue<(bool visible, Action callback)> orders = new Queue<(bool, Action)>();
    private Vector3 fullScale = Vector3.one;
    private float progress = 1f; // 0 = hidden, 1 = fully visible
    private bool initialized;
    private Coroutine currentRoutine;

    // ---------- Public API ----------

    public bool isAnimating => currentRoutine != null;

    /// <summary>Last requested state (what the object will be once the queue is done).</summary>
    public bool isVisible { get; private set; } = true;

    public float animationDuration { get => duration; set => duration = Mathf.Max(0f, value); }
    public AnimationCurve animationCurve { get => scaleCurve; set => scaleCurve = value ?? createExponentialEaseOutCurve(); }

    // Parameterless overloads are the ones visible in UnityEvent / Button inspectors.
    public void scaleIn() => enqueueOrder(true, null);
    public void scaleOut() => enqueueOrder(false, null);
    public void scaleIn(Action onComplete) => enqueueOrder(true, onComplete);
    public void scaleOut(Action onComplete) => enqueueOrder(false, onComplete);

    /// <summary>Cancels the current transition and the queue, then snaps to visible / hidden.</summary>
    public void setVisibleInstant(bool visible)
    {
        ensureInitialized();
        cancelAll();
        isVisible = visible;
        setProgress(visible ? 1f : 0f);
    }

    // ---------- Unity messages ----------

    private void Awake() => ensureInitialized();

    private void OnEnable()
    {
        ensureInitialized();
        if (!playOnEnable) return;
        setProgress(0f);
        scaleIn();
    }

    private void OnDisable()
    {
        // Coroutines die with the object: drop the queue and snap to the final state so it never stays half scaled.
        if (currentRoutine == null) return;
        cancelAll();
        setProgress(isVisible ? 1f : 0f);
    }

    // ---------- Internals ----------

    private void enqueueOrder(bool visible, Action callback)
    {
        ensureInitialized();
        isVisible = visible;
        if (visible && !gameObject.activeSelf) gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy) // coroutines can't run under an inactive parent: apply instantly
        {
            if (visible) Debug.LogWarning($"[ScaleInOut] '{name}' has an inactive parent, applying scale instantly.", this);
            setProgress(visible ? 1f : 0f);
            (visible ? onScaleInFinished : onScaleOutFinished)?.Invoke();
            callback?.Invoke();
            return;
        }

        orders.Enqueue((visible, callback));
        if (currentRoutine == null) currentRoutine = StartCoroutine(runOrders());
    }

    private IEnumerator runOrders()
    {
        while (orders.Count > 0)
        {
            (bool visible, Action callback) = orders.Dequeue();
            float target = visible ? 1f : 0f;
            float start = progress;
            float total = duration * Mathf.Abs(target - start); // constant speed even from a partial state
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                setProgress(Mathf.Lerp(start, target, elapsed / total)); // Lerp clamps t
                yield return null;
            }

            setProgress(target);
            (visible ? onScaleInFinished : onScaleOutFinished)?.Invoke();
            callback?.Invoke();

            if (!visible && deactivateOnScaleOut && orders.Count == 0)
            {
                currentRoutine = null; // so OnDisable doesn't treat this as an interruption
                gameObject.SetActive(false);
                yield break;
            }
        }

        currentRoutine = null;
    }

    private void cancelAll()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = null;
        orders.Clear();
    }

    private void ensureInitialized()
    {
        if (initialized) return;
        initialized = true;

        fullScale = transform.localScale;
        // Left at scale 0 in the editor? Then "full scale" would be 0 and it would never show up.
        if (fullScale.sqrMagnitude < 0.0001f) fullScale = Vector3.one;

        isVisible = !startHidden;
        setProgress(startHidden ? 0f : 1f);
    }

    private void setProgress(float newProgress)
    {
        progress = newProgress;
        transform.localScale = fullScale * Mathf.Max(0f, scaleCurve.Evaluate(progress));
    }

    /// <summary>1 - e^(-k t) normalized to end exactly at 1: fast start, soft landing. Analytic tangents keep it smooth.</summary>
    private static AnimationCurve createExponentialEaseOutCurve()
    {
        const int samples = 12;
        const float k = 5f;
        float norm = 1f - Mathf.Exp(-k);

        Keyframe[] keys = new Keyframe[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            float tangent = k * Mathf.Exp(-k * t) / norm;
            keys[i] = new Keyframe(t, (1f - Mathf.Exp(-k * t)) / norm, tangent, tangent);
        }

        return new AnimationCurve(keys);
    }
}