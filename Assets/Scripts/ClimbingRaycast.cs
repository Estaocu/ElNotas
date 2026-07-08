using UnityEngine;
using CMF;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class ClimbingRaycast : MonoBehaviour
{
    [Header("Eye ray (forward)")]
    public Vector3 eyeRayStart;
    public float eyeRayLength = 1f;

    [Header("Up ray")]
    [Tooltip("How far ahead of the eye ray's impact point the up ray starts.")]
    public float upRayForwardOffset = 0.2f;

    [Tooltip("Layers considered climbable geometry. Exclude Player, Feet and AbyssRaycast so the rays ignore the player's own colliders.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Teleport (debug)")]
    [Tooltip("Vertical offset applied on teleport so the player stands on the surface instead of clipping into it.")]
    public float standOffset = 0f;

    [Tooltip("Player controller, used to reset momentum on teleport so CMF doesn't fling the player afterwards.")]
    [SerializeField] private AdvancedWalkerController walker;

    [Tooltip("Player mover, used to only allow climbing while the player is grounded.")]
    [SerializeField] private Mover mover;

    [Header("Climb height tiers")]
    [Tooltip("Upper bound (in meters) of each climb tier. A climb taller than the highest threshold is considered too high to climb.")]
    public float lowClimbThreshold = 0.5f;
    public float midClimbThreshold = 2f;
    public float highClimbThreshold = 3f;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private float durationToCheck = 3.0f;
    private float timer = 0.0f;
    private bool isChecking = false;
    private bool inputFailed = false;
    private Vector3 targetClimbPoint = Vector3.zero;

    public enum ClimbTier { Low, Mid, High, TooHigh }

    // Distance from the up ray spawn (ground level) to its hit point, i.e. the height of the current climb;
    [SerializeField] private float climbHeight;

    // Length used only to draw the 'infinite' up ray as a gizmo/debug line;
    const float upRayGizmoLength = 1000f;

    // One-shot guard: re-arms only when the up ray stops hitting, so we don't teleport every physics frame;
    bool canTeleport = true;

    void Update()
    {
        if (isChecking)
        {
            UpdateInputCheck();
        }  
    }

    void OnDrawGizmos()
    {
        if (transform.parent == null)
        {
            return;
        }

        Vector3 worldEyeRayStart = transform.parent.TransformPoint(eyeRayStart);
        Vector3 forward = transform.parent.forward;

        // Eye ray (forward);
        bool eyeHitSomething = Physics.Raycast(worldEyeRayStart, forward, out RaycastHit eyeHit, eyeRayLength, groundMask, QueryTriggerInteraction.Ignore);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(worldEyeRayStart, worldEyeRayStart + forward * eyeRayLength);

        if (!eyeHitSomething)
        {
            return;
        }

        Gizmos.DrawSphere(eyeHit.point, 0.05f);

        // Up ray: starts a bit ahead of the eye impact point but at ground/foot level (not eye level), and goes infinitely up until it hits something;
        // Temporarily enable backface queries so it also hits mesh faces whose normals are flipped away from the ray;
        Vector3 worldUpRayStart = eyeHit.point + forward * upRayForwardOffset;
        worldUpRayStart.y = transform.position.y;
        bool prevQueriesHitBackfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        bool upHitSomething = Physics.Raycast(worldUpRayStart, Vector3.up, out RaycastHit upHit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore);
        Physics.queriesHitBackfaces = prevQueriesHitBackfaces;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldUpRayStart, worldUpRayStart + Vector3.up * upRayGizmoLength);

        if (upHitSomething)
        {
            // Fixed typo: was drawing sphere at old unassigned upHit out of context
            Gizmos.DrawSphere(upHit.point, 0.05f);
        }
    }

    void FixedUpdate()
    {
        if (transform.parent == null)
        {
            return;
        }

        Vector3 worldEyeRayStart = transform.parent.TransformPoint(eyeRayStart);
        Vector3 forward = transform.parent.forward;

        // Eye ray (forward);
        bool eyeHitSomething = Physics.Raycast(worldEyeRayStart, forward, out RaycastHit eyeHit, eyeRayLength, groundMask, QueryTriggerInteraction.Ignore);

        bool upHitSomething = false;
        RaycastHit upHit = default;

        if (eyeHitSomething)
        {
            Debug.DrawRay(worldEyeRayStart, forward * eyeRayLength, Color.green);

            // Up ray: starts a bit ahead of the eye impact point but at ground/foot level (not eye level), and goes infinitely up until it hits something;
            // Temporarily enable backface queries so it also hits mesh faces whose normals are flipped away from the ray;
            Vector3 worldUpRayStart = eyeHit.point + forward * upRayForwardOffset;
            worldUpRayStart.y = transform.position.y;
            bool prevQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            upHitSomething = Physics.Raycast(worldUpRayStart, Vector3.up, out upHit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = prevQueriesHitBackfaces;

            Debug.DrawRay(worldUpRayStart, Vector3.up * upRayGizmoLength, Color.yellow);

            // Save the climb height: distance from the up ray spawn (ground level) to the hit;
            if (upHitSomething)
                climbHeight = upHit.distance;
        }

        // Teleport the player to the up ray's impact point (one-shot until the ray stops hitting);
        // Only allow climbing while grounded, and skip climbs that are too high;
        if (upHitSomething)
        {
            if (canTeleport && mover.IsGrounded())
            {
                ClimbTier tier = GetClimbTier(climbHeight);
                Debug.Log($"[ClimbingRaycast] Climb height {climbHeight:F2}m -> tier {tier}");

                if (tier != ClimbTier.TooHigh)
                {
                    // Start timer and keep checking if player is inputting forward
                    StartInputCheck(upHit.point);
                }
                
                canTeleport = false;
            }
        }
        else
        {
            // Re-arm whenever the up ray is not hitting (eye miss OR up miss);
            canTeleport = true;
        }
    }

    // Classify a climb height into a tier based on the thresholds (used later to pick the climb animation);
    ClimbTier GetClimbTier(float height)
    {
        if (height <= lowClimbThreshold) return ClimbTier.Low;
        if (height <= midClimbThreshold) return ClimbTier.Mid;
        if (height <= highClimbThreshold) return ClimbTier.High;
        return ClimbTier.TooHigh;
    }

    // Teleport the player body to a point, killing momentum so CMF doesn't fling it afterwards;
    void TeleportPlayer(Vector3 point)
    {
        Vector3 target = point + Vector3.up * standOffset;

        // Resolve the body to move: prefer the assigned walker, else the parent, else this object;
        Transform body = walker != null ? walker.transform
                       : transform.parent != null ? transform.parent
                       : transform;

        Debug.Log($"[ClimbingRaycast] Teleport '{body.name}' from {body.position} to {target}", body);

        body.position = target;

        // Keep the Rigidbody in sync so interpolation doesn't drag the visual across the map;
        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = target;
            rb.velocity = Vector3.zero;
        }

        if (walker != null)
            walker.SetMomentum(Vector3.zero);
    }

    public void StartInputCheck(Vector3 hitPoint)
    {
        timer = 0.0f;
        isChecking = true;
        inputFailed = false;
        targetClimbPoint = hitPoint;
        Debug.Log("Started checking for forward input.");
    }

    private void UpdateInputCheck()
    {
        // Vector2 reads both Keyboard (WASD) and Gamepad (Left Stick)
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        // Check if the Y axis is positive (W key or pushing the stick upward)
        // A small deadzone (0.5f) ensures the stick is intentionally pushed forward
        bool isForwardActive = moveInput.y > 0.5f;

        if (!isForwardActive)
        {
            inputFailed = true;
            isChecking = false;
            Debug.Log("Forward input interrupted. Check failed.");
            return;
        }

        timer += Time.deltaTime;

        if (timer >= durationToCheck)
        {
            isChecking = false;
            Debug.Log("Success: Forward input was held continuously for " + durationToCheck + " seconds.");
            TeleportPlayer(targetClimbPoint);
        }
    }
}