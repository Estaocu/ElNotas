using UnityEngine;

/// <summary>
/// Universal rubberhose-style squash & stretch bounce for a visual (mesh) transform.
/// Call playBounce() from any event (UnityEvent in the inspector, or from code:
///   worldTick += bouncer.playBounce;   // OnEnable
///   worldTick -= bouncer.playBounce;   // OnDisable
/// ).
/// IMPORTANT: only the scale of "visualTarget" is touched. Keep colliders / rigidbody
/// on a different transform (a parent, or a sibling), never on the target or its children.
/// </summary>
[DisallowMultipleComponent]
public class RubberhoseBounce : MonoBehaviour
{
    public enum AxisMode { verticalOnly, allAxes }
    public enum RetriggerMode { restart, ignoreWhilePlaying }

    // Lower bound for the scale factor so a big amplitude can never flip the mesh (negative scale).
    private const float minScaleFactor = 0.05f;

    [Header("Target")]
    [Tooltip("Transform holding ONLY the mesh. If empty, the first MeshRenderer found in children (or on this object) is used.")]
    [SerializeField] private Transform visualTarget;

    [Header("Axes")]
    [SerializeField] private AxisMode axisMode = AxisMode.verticalOnly;
    [Tooltip("Vertical Only mode: 1 = X/Z compensate so the volume stays constant (classic squash & stretch). 0 = only Y changes.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumePreservation = 1f;

    [Header("Bounce")]
    [Tooltip("Max scale deviation. 0.3 = up to +-30% of the base scale.")]
    [Min(0f)]
    [SerializeField] private float amplitude = 0.3f;
    [Tooltip("Total duration of one bounce, in seconds.")]
    [Min(0.01f)]
    [SerializeField] private float duration = 0.6f;
    [Tooltip("Number of full stretch+squash cycles inside the duration.")]
    [Min(0.5f)]
    [SerializeField] private float bounceCount = 3f;
    [Tooltip("True: first moves taller/bigger. False: first squashes.")]
    [SerializeField] private bool startWithStretch = true;
    [Tooltip("Envelope over the normalized duration (X: 0..1, Y: strength). Default fades out 1 -> 0.")]
    [SerializeField] private AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Behaviour")]
    [Tooltip("What happens if playBounce() is called while a bounce is still running.")]
    [SerializeField] private RetriggerMode retriggerMode = RetriggerMode.restart;
    [Tooltip("Ignore Time.timeScale (useful with pause / slow motion).")]
    [SerializeField] private bool useUnscaledTime = false;

    private Vector3 baseScale = Vector3.one;
    private float elapsedTime;
    private bool isPlaying;
    private bool hasWarnedMissingTarget;
    private RhythmClock clock;


    // ------------------------------------------------------------------ Unity callbacks

    private void Reset()
    {
        visualTarget = findDefaultTarget();
    }

    private void Awake()
    {
        clock = FindFirstObjectByType<RhythmClock>();
        if (visualTarget == null) visualTarget = findDefaultTarget();
        warnIfTargetAffectsColliders();
    }

    private void OnEnable()
    {
        RhythmClock.OnBeat += playBounce;

    }


    private void OnDisable()
    {
        // Leave the mesh exactly as it was if the component / object is disabled mid-bounce.
        stopBounce();
        RhythmClock.OnBeat -= playBounce;
    }

    private void OnValidate()
    {
        amplitude = Mathf.Max(0f, amplitude);
        duration = Mathf.Max(0.01f, duration);
        bounceCount = Mathf.Max(0.5f, bounceCount);
    }

    private void Update()
    {
        if (!isPlaying) return;

        // Target destroyed mid-bounce.
        if (visualTarget == null)
        {
            isPlaying = false;
            return;
        }

        elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (elapsedTime >= duration)
        {
            stopBounce();
            return;
        }

        applyScale(elapsedTime / duration);
    }

    // ------------------------------------------------------------------ Public API

    /// <summary>Hook this to your periodic event.</summary>
    private void playBounce(RhythmTick tick)
    {
        if (visualTarget == null)
        {
            if (!hasWarnedMissingTarget)
            {
                Debug.LogWarning($"[{nameof(RubberhoseBounce)}] No visualTarget assigned on '{name}'.", this);
                hasWarnedMissingTarget = true;
            }
            return;
        }

        if (isPlaying && retriggerMode == RetriggerMode.ignoreWhilePlaying) return;

        // Re-read the base scale only when idle, so external scale changes between bounces are respected
        // and a restart never accumulates deformation.
        if (!isPlaying) baseScale = visualTarget.localScale;

        elapsedTime = 0f;
        isPlaying = true;
    }

    /// <summary>Stops immediately and restores the base scale.</summary>
    public void stopBounce()
    {
        if (isPlaying && visualTarget != null) visualTarget.localScale = baseScale;
        isPlaying = false;
        elapsedTime = 0f;
    }

    public void setAmplitude(float newAmplitude) { amplitude = Mathf.Max(0f, newAmplitude); }
    public void setDuration(float newDuration) { duration = Mathf.Max(0.01f, newDuration); }
    public void setBounceCount(float newBounceCount) { bounceCount = Mathf.Max(0.5f, newBounceCount); }

    // ------------------------------------------------------------------ Internals

    private void applyScale(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float envelope = decayCurve.Evaluate(t);
        float wave = Mathf.Sin(t * bounceCount * Mathf.PI * 2f);
        float direction = startWithStretch ? 1f : -1f;

        float offset = amplitude * envelope * wave * direction;
        float factor = Mathf.Max(minScaleFactor, 1f + offset);

        Vector3 newScale;
        if (axisMode == AxisMode.allAxes)
        {
            newScale = baseScale * factor;
        }
        else
        {
            // factor * horizontal^2 = 1 when volumePreservation = 1
            float horizontalFactor = Mathf.Pow(factor, -0.5f * volumePreservation);
            newScale = new Vector3(
                baseScale.x * horizontalFactor,
                baseScale.y * factor,
                baseScale.z * horizontalFactor);
        }

        visualTarget.localScale = newScale;
    }

    private Transform findDefaultTarget()
    {
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>(true);
        return meshRenderer != null ? meshRenderer.transform : null;
    }

    private void warnIfTargetAffectsColliders()
    {
        if (visualTarget == null) return;

        // Colliders on the target or on any of its children get scaled together with it.
        bool hasCollider3D = visualTarget.GetComponentInChildren<Collider>(true) != null;
        bool hasCollider2D = visualTarget.GetComponentInChildren<Collider2D>(true) != null;

        if (hasCollider3D || hasCollider2D)
        {
            Debug.LogWarning(
                $"[{nameof(RubberhoseBounce)}] '{visualTarget.name}' (or a child) has a collider, so it will be scaled too. " +
                "Move the mesh to its own child object and assign that as visualTarget.", this);
        }
    }
}