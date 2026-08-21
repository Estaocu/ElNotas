using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    public UnityEvent<GameObject> OnGameObjectDetected;
    public UnityEvent<GameObject> OnGameObjectLost;

    public GameObject LastDetected { get; private set; }

    private bool hasFired;
    private readonly Dictionary<GameObject, int> activeCounts = new Dictionary<GameObject, int>();

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasFired) return;
        if (!PassesFilters(other)) return;

        GameObject root = ResolveRoot(other);
        if (activeCounts.TryGetValue(root, out int count))
        {
            activeCounts[root] = count + 1;
            return;
        }
        activeCounts[root] = 1;

        LastDetected = root;
        hasFired = true;

        if (debugLog) Debug.Log($"[TriggerDetector] {name} detected {root.name}", this);

        OnDetected?.Invoke();
        OnGameObjectDetected?.Invoke(root);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject root = ResolveRoot(other);
        if (!activeCounts.TryGetValue(root, out int count)) return;
        if (count > 1)
        {
            activeCounts[root] = count - 1;
            return;
        }
        activeCounts.Remove(root);

        if (LastDetected != root) return;
        if (debugLog) Debug.Log($"[TriggerDetector] {name} lost {root.name}", this);

        GameObject previousDetected = LastDetected;
        LastDetected = null;

        OnLost?.Invoke();
        OnGameObjectLost?.Invoke(previousDetected);
    }

    private void OnDisable()
    {
        activeCounts.Clear();
        if (LastDetected == null) return;

        GameObject previousDetected = LastDetected;
        LastDetected = null;

        OnLost?.Invoke();
        OnGameObjectLost?.Invoke(previousDetected);
    }

    public void OnSpawn()
    {
        activeCounts.Clear();
        LastDetected = null;
        hasFired = false;
    }

    private static GameObject ResolveRoot(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
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
        Collider col = GetComponent<Collider>();
        // if (col != null && !col.isTrigger)
        //     Debug.LogWarning($"[TriggerDetector] Collider on '{name}' is not set as Trigger.", this);
        if (layerMask.value == 0)
            Debug.LogWarning($"[TriggerDetector] LayerMask on '{name}' is empty — nothing will be detected.", this);
    }
#endif
}