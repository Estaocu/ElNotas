using UnityEngine;
using CMF; // Namespace of Character Movement Fundamentals

[RequireComponent(typeof(Collider))]
public class LoadingZone : MonoBehaviour
{
    [Header("Target Location Settings")]
    [Tooltip("Child Transform that defines the teleport position and orientation.")]
    [SerializeField] private Transform targetDestination;

    [Header("Abilities References")]
    [Tooltip("Reference to the AbyssJump component to disable during teleport.")]
    [SerializeField] private Behaviour abyssJumpScript;

    [Tooltip("Reference to the climbing component to disable during teleport.")]
    [SerializeField] private Behaviour climbScript;

    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.8f);
    [SerializeField] private float gizmoRadius = 0.5f;

    private void Awake()
    {
        if (targetDestination == null)
        {
            Debug.LogWarning($"[{nameof(LoadingZone)}] No 'targetDestination' assigned in {gameObject.name}.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetDestination == null) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<Mover>() != null)
        {
            TeleportPlayer(other.gameObject);
        }
    }

    private void TeleportPlayer(GameObject playerObj)
    {
        // 1. Disable movement abilities prior to teleportation
        SetAbilitiesState(false);

        // 2. Fetch CMF components
        Mover mover = playerObj.GetComponentInParent<Mover>();
        GameObject playerRoot = mover != null ? mover.gameObject : playerObj;

        // 3. Reset physics velocity
        if (mover != null)
        {
            mover.SetVelocity(Vector3.zero);
        }

        // 4. Update position and rotation
        playerRoot.transform.position = targetDestination.position;
        playerRoot.transform.rotation = targetDestination.rotation;

        // 5. Force physics engine transform synchronization
        Physics.SyncTransforms();

        // 6. Force immediate ground check
        if (mover != null)
        {
            mover.CheckForGround();
        }

        // 7. Re-enable movement abilities after location is settled
        SetAbilitiesState(true);

        Debug.Log($"[{nameof(LoadingZone)}] Player successfully teleported to: {targetDestination.position}");
    }

    private void SetAbilitiesState(bool isEnabled)
    {
        if (abyssJumpScript != null)
        {
            abyssJumpScript.enabled = isEnabled;
        }

        if (climbScript != null)
        {
            climbScript.enabled = isEnabled;
        }
    }

    #region Editor Visualization (Gizmos)

    private void OnDrawGizmos()
    {
        if (targetDestination == null) return;
        DrawDestinationGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (targetDestination == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetDestination.position);

        DrawDestinationGizmo();
    }

    private void DrawDestinationGizmo()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(targetDestination.position, gizmoRadius);

        Vector3 forward = targetDestination.forward * (gizmoRadius * 2f);
        Gizmos.DrawRay(targetDestination.position, forward);
    }

    #endregion
}