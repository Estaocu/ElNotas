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

    [Header("Surface Validation")]
    [Tooltip("Maximum allowed angle (in degrees) between the surface normal and Vector3.up to be considered a valid floor.")]
    [Range(0f, 60f)]
    [SerializeField] private float maxGroundAngle = 30f;

    [Tooltip("Maximum allowed horizontal distance (in meters) between player and climb target to execute teleport.")]
    [SerializeField] private float maxClimbDistance = 2f;

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

    [Header("Climb input")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("How long the player must continuously hold movement toward the wall.")]
    [SerializeField] private float durationToCheck = 3f;

    [Tooltip("Minimum input direction alignment required to climb. 1 = exactly toward the wall, 0 = any direction.")]
    [Range(0f, 1f)]
    [SerializeField] private float climbInputThreshold = 0.5f;

    private float timer = 0f;
    private bool isChecking = false;
    private Vector3 targetClimbPoint = Vector3.zero;
    private Vector3 climbInputDirection = Vector3.zero;

    [SerializeField] private AbyssRaycast abyssCast;

    public enum ClimbTier
    {
        Low,
        Mid,
        High,
        TooHigh
    }

    [SerializeField] private float climbHeight;

    private const float upRayGizmoLength = 1000f;
    private bool canTeleport = true;

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

        bool eyeHitSomething = Physics.Raycast(
            worldEyeRayStart,
            forward,
            out RaycastHit eyeHit,
            eyeRayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            worldEyeRayStart,
            worldEyeRayStart + forward * eyeRayLength
        );

        if (!eyeHitSomething)
        {
            return;
        }

        Gizmos.DrawSphere(eyeHit.point, 0.05f);

        Vector3 worldUpRayStart = eyeHit.point + forward * upRayForwardOffset;
        worldUpRayStart.y = transform.position.y;

        bool previousQueriesHitBackfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        bool upHitSomething = Physics.Raycast(
            worldUpRayStart,
            Vector3.up,
            out RaycastHit upHit,
            Mathf.Infinity,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        Physics.queriesHitBackfaces = previousQueriesHitBackfaces;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            worldUpRayStart,
            worldUpRayStart + Vector3.up * upRayGizmoLength
        );

        if (upHitSomething)
        {
            float surfaceAngle = Vector3.Angle(upHit.normal, Vector3.up);
            Gizmos.color = surfaceAngle <= maxGroundAngle ? Color.cyan : Color.red;
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

        bool eyeHitSomething = Physics.Raycast(
            worldEyeRayStart,
            forward,
            out RaycastHit eyeHit,
            eyeRayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        bool upHitSomething = false;
        RaycastHit upHit = default;

        if (eyeHitSomething)
        {
            Debug.DrawRay(
                worldEyeRayStart,
                forward * eyeRayLength,
                Color.green
            );

            Vector3 worldUpRayStart = eyeHit.point + forward * upRayForwardOffset;
            worldUpRayStart.y = transform.position.y;

            bool previousQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            upHitSomething = Physics.Raycast(
                worldUpRayStart,
                Vector3.up,
                out upHit,
                Mathf.Infinity,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            Physics.queriesHitBackfaces = previousQueriesHitBackfaces;

            Debug.DrawRay(
                worldUpRayStart,
                Vector3.up * upRayGizmoLength,
                Color.yellow
            );

            if (upHitSomething)
            {
                climbHeight = upHit.distance;
            }
        }
        else
        {
            CancelInputCheck();
        }

        if (upHitSomething)
        {
            float surfaceAngle = Vector3.Angle(upHit.normal, Vector3.up);
            bool isValidSurface = surfaceAngle <= maxGroundAngle;

            if (canTeleport && isValidSurface && mover != null && mover.IsGrounded())
            {
                ClimbTier tier = GetClimbTier(climbHeight);

                if (tier != ClimbTier.TooHigh)
                {
                    StartInputCheck(upHit.point, eyeHit.point);
                }

                canTeleport = false;
            }
        }
        else
        {
            canTeleport = true;
        }
    }

    ClimbTier GetClimbTier(float height)
    {
        if (height <= lowClimbThreshold)
        {
            return ClimbTier.Low;
        }

        if (height <= midClimbThreshold)
        {
            return ClimbTier.Mid;
        }

        if (height <= highClimbThreshold)
        {
            return ClimbTier.High;
        }

        return ClimbTier.TooHigh;
    }

    void TeleportPlayer(Vector3 point)
    {
        Vector3 target = point + Vector3.up * standOffset;

        Transform body = GetBodyTransform();

        body.position = target;

        Rigidbody rb = body.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position = target;
            rb.velocity = Vector3.zero;
        }

        if (walker != null)
        {
            walker.SetMomentum(Vector3.zero);
        }

        Physics.SyncTransforms();

        if (mover != null)
        {
            mover.CheckForGround();
        }
    }

    public void StartInputCheck(Vector3 hitPoint, Vector3 wallPoint)
    {
        timer = 0f;
        isChecking = true;
        targetClimbPoint = hitPoint;

        Transform body = GetBodyTransform();

        climbInputDirection = wallPoint - body.position;
        climbInputDirection.y = 0f;

        if (climbInputDirection.sqrMagnitude > 0.0001f)
        {
            climbInputDirection.Normalize();
        }
        else
        {
            CancelInputCheck();
            return;
        }
    }

    private void CancelInputCheck()
    {
        timer = 0f;
        isChecking = false;
        targetClimbPoint = Vector3.zero;
        climbInputDirection = Vector3.zero;
    }

    private void UpdateInputCheck()
    {
        Transform body = GetBodyTransform();

        Vector3 flatPlayerPos = Vector3.Scale(body.position, new Vector3(1, 0, 1));
        Vector3 flatTargetPos = Vector3.Scale(targetClimbPoint, new Vector3(1, 0, 1));

        if (Vector3.Distance(flatPlayerPos, flatTargetPos) > maxClimbDistance)
        {
            CancelInputCheck();
            return;
        }

        if (moveAction == null || moveAction.action == null)
        {
            CancelInputCheck();
            return;
        }

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude < 0.0001f)
        {
            timer = 0f;
            return;
        }

        Vector3 inputDirection = GetWorldInputDirection(moveInput);

        if (inputDirection.sqrMagnitude < 0.0001f)
        {
            timer = 0f;
            return;
        }

        float directionAlignment = Vector3.Dot(
            inputDirection.normalized,
            climbInputDirection
        );

        if (directionAlignment < climbInputThreshold)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= durationToCheck)
        {
            Vector3 destination = targetClimbPoint;

            CancelInputCheck();

            TeleportPlayer(destination);

            if (abyssCast != null)
            {
                abyssCast.NotifyClimbCompleted();
            }
        }
    }

    private Transform GetBodyTransform()
    {
        return walker != null
            ? walker.transform
            : transform.parent != null
                ? transform.parent
                : transform;
    }

    private Vector3 GetWorldInputDirection(Vector2 moveInput)
    {
        Transform body = GetBodyTransform();

        Vector3 inputDirection;

        if (walker != null && walker.cameraTransform != null)
        {
            Vector3 cameraRight = Vector3.ProjectOnPlane(
                walker.cameraTransform.right,
                body.up
            ).normalized;

            Vector3 cameraForward = Vector3.ProjectOnPlane(
                walker.cameraTransform.forward,
                body.up
            ).normalized;

            inputDirection =
                cameraRight * moveInput.x +
                cameraForward * moveInput.y;
        }
        else
        {
            inputDirection =
                body.right * moveInput.x +
                body.forward * moveInput.y;
        }

        inputDirection = Vector3.ProjectOnPlane(
            inputDirection,
            body.up
        );

        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            inputDirection.Normalize();
        }

        return inputDirection;
    }

    public bool IsCheckingClimb()
    {
        return isChecking;
    }

    public bool IsEyeRayHitting()
    {
        if (transform.parent == null)
        {
            return false;
        }

        Vector3 worldEyeRayStart = transform.parent.TransformPoint(eyeRayStart);
        Vector3 forward = transform.parent.forward;

        return Physics.Raycast(
            worldEyeRayStart,
            forward,
            eyeRayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
}