using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Modular trigger detector. Place on a child GameObject with a trigger Collider.
/// Wire OnDetected / OnLost from the Inspector to invoke any parameterless method.
/// Listeners that need to know what entered should read <see cref="LastDetected"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerDetector : MonoBehaviour, IPoolable
{
    [Header("Filters")]
    [SerializeField] private LayerMask layerMask = ~0;
    [SerializeField] private string requiredTag = "";

    [Header("Behavior")]
    [SerializeField] private bool triggerOnce = false;
    [SerializeField] private bool debugLog = false;

    [Header("Events")]
    public UnityEvent OnDetected;
    public UnityEvent OnLost;

    // Legacy serialized fields kept ONLY so the migration tool can locate orphan
    // persistent listeners from the previous UnityEvent<Collider>/<GameObject> API.
    // These are never invoked. Remove after migration is complete on all assets.
    [HideInInspector, SerializeField] private UnityEvent onTriggerEntered;
    [HideInInspector, SerializeField] private UnityEvent onTriggerExited;

    public GameObject LastDetected { get; private set; }

    private bool hasFired;
    // Per-entity collider counts. Compound colliders (multiple child colliders sharing
    // one Rigidbody) fire OnTriggerEnter/Exit per collider; we coalesce by Rigidbody
    // root so OnDetected/OnLost fire once per logical entity.
    private readonly Dictionary<GameObject, int> activeCounts = new();

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasFired) return;
        if (!PassesFilters(other)) return;

        var root = ResolveRoot(other);
        if (activeCounts.TryGetValue(root, out var n))
        {
            activeCounts[root] = n + 1;
            return;
        }
        activeCounts[root] = 1;

        LastDetected = root;
        hasFired = true;

        if (debugLog) Debug.Log($"[TriggerDetector] {name} detected {root.name}", this);
        OnDetected?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        var root = ResolveRoot(other);
        if (!activeCounts.TryGetValue(root, out var n)) return;
        if (n > 1)
        {
            activeCounts[root] = n - 1;
            return;
        }
        activeCounts.Remove(root);

        if (LastDetected != root) return;
        if (debugLog) Debug.Log($"[TriggerDetector] {name} lost {root.name}", this);
        LastDetected = null;
        OnLost?.Invoke();
    }

    private void OnDisable()
    {
        activeCounts.Clear();
        if (LastDetected == null) return;
        LastDetected = null;
        OnLost?.Invoke();
    }

    public void OnSpawn()
    {
        activeCounts.Clear();
        LastDetected = null;
        hasFired = false;
    }

    private static GameObject ResolveRoot(Collider other)
    {
        var rb = other.attachedRigidbody;
        return rb != null ? rb.gameObject : other.gameObject;
    }

    private bool PassesFilters(Collider other)
    {
        if ((layerMask.value & (1 << other.gameObject.layer)) == 0) return false;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return false;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[TriggerDetector] Collider on '{name}' is not set as Trigger.", this);
        if (layerMask.value == 0)
            Debug.LogWarning($"[TriggerDetector] LayerMask on '{name}' is empty — nothing will be detected.", this);
    }

    // private void OnDrawGizmosSelected()
    // {
    //     var col = GetComponent<Collider>();
    //     if (col == null) return;
    //     Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
    //     Gizmos.matrix = transform.localToWorldMatrix;
    //     switch (col)
    //     {
    //         case BoxCollider box:
    //             Gizmos.DrawCube(box.center, box.size);
    //             break;
    //         case SphereCollider sph:
    //             Gizmos.DrawSphere(sph.center, sph.radius);
    //             break;
    //         default:
    //             var b = col.bounds;
    //             Gizmos.matrix = Matrix4x4.identity;
    //             Gizmos.DrawCube(b.center, b.size);
    //             break;
    //     }
    // }
#endif
}
