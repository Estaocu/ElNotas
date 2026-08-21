using UnityEngine;

[ExecuteInEditMode]
public class AbyssRaycast : MonoBehaviour
{
    public Vector3 ray1Start;
    public Vector3 ray2Start;
    public float rayLength;

    private bool canJump = true;

    [Header("Jump Requirements")]
    [SerializeField] private float minJumpSpeed;

    [Tooltip("Minimum time the player must continuously hold movement before an abyss jump can trigger.")]
    [SerializeField] private float minMovementHoldTime = 0.25f;

    [Tooltip("Minimum movement input magnitude required to start or continue the movement hold timer.")]
    [SerializeField] private float movementInputThreshold = 0.1f;

    [Tooltip("Maximum allowed change in movement direction before the movement hold timer is reset.")]
    [Range(-1f, 1f)]
    [SerializeField] private float movementDirectionThreshold = 0.5f;

    [Header("Climb Protection")]
    [Tooltip("Time that must pass after climbing before an abyss rescue can trigger again.")]
    [SerializeField] private float abyssJumpCooldownAfterClimb = 0.5f;

    [SerializeField] private CMF.Mover mover;
    [SerializeField] private CMF.AdvancedWalkerController walker;

    [Tooltip("Layers considered ground. Exclude Player, Feet and AbyssRaycast so the rays ignore the player's own colliders.")]
    [SerializeField] private LayerMask groundMask = ~0;

    public float jumpApexHeight = 2f;
    public float jumpForwardDistance = 4f;

    [Header("Climbing Reference")]
    [SerializeField] private ClimbingRaycast climbingCast;

    private float movementHoldTimer = 0f;
    private Vector3 heldMovementDirection = Vector3.zero;

    private float abyssJumpCooldownTimer = 0f;

    void OnEnable()
    {
        if (Application.isPlaying && walker != null)
        {
            walker.OnLand += HandleLand;
        }
    }

    void OnDisable()
    {
        if (walker != null)
        {
            walker.OnLand -= HandleLand;
        }
    }

    void HandleLand(Vector3 collisionVelocity)
    {
        canJump = true;
        movementHoldTimer = 0f;
        heldMovementDirection = Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (transform.parent == null)
        {
            return;
        }

        Vector3 worldRay1Start = transform.parent.TransformPoint(ray1Start);
        Vector3 worldRay2Start = transform.parent.TransformPoint(ray2Start);

        DrawRayGizmo(worldRay1Start, Color.magenta);
        DrawRayGizmo(worldRay2Start, Color.blue);
    }

    void DrawRayGizmo(Vector3 origin, Color color)
    {
        bool hitSomething = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            rayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        Gizmos.color = color;

        Gizmos.DrawLine(
            origin,
            origin + Vector3.down * rayLength
        );

        if (hitSomething)
        {
            Gizmos.DrawSphere(hit.point, 0.05f);
        }
    }

    void FixedUpdate()
    {
        if (transform.parent == null || mover == null || walker == null)
        {
            return;
        }

        UpdateAbyssJumpCooldown();
        UpdateMovementHoldTimer();

        Vector3 worldRay1Start = transform.parent.TransformPoint(ray1Start);
        Vector3 worldRay2Start = transform.parent.TransformPoint(ray2Start);

        bool ray1DetectsGround = Physics.Raycast(
            worldRay1Start,
            Vector3.down,
            rayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        bool ray2DetectsGround = Physics.Raycast(
            worldRay2Start,
            Vector3.down,
            rayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawRay(
            worldRay1Start,
            Vector3.down * rayLength,
            ray1DetectsGround ? Color.green : Color.magenta
        );

        Debug.DrawRay(
            worldRay2Start,
            Vector3.down * rayLength,
            ray2DetectsGround ? Color.green : Color.blue
        );

        bool bothRaysDetectAbyss =
            !ray1DetectsGround &&
            !ray2DetectsGround;

        if (!bothRaysDetectAbyss)
        {
            return;
        }

        if (!mover.IsGrounded() || !canJump)
        {
            return;
        }

        if (abyssJumpCooldownTimer > 0f)
        {
            return;
        }

        // Never trigger an abyss rescue while the climbing system is checking for a climb.
        if (climbingCast != null && climbingCast.IsCheckingClimb())
        {
            return;
        }

        float currentSpeed = GetHorizontalSpeed();

        if (currentSpeed < minJumpSpeed)
        {
            return;
        }

        if (movementHoldTimer < minMovementHoldTime)
        {
            return;
        }

        if (heldMovementDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        LaunchParabolicJump();

        canJump = false;
    }

    void UpdateAbyssJumpCooldown()
    {
        if (abyssJumpCooldownTimer <= 0f)
        {
            abyssJumpCooldownTimer = 0f;
            return;
        }

        abyssJumpCooldownTimer -= Time.fixedDeltaTime;

        if (abyssJumpCooldownTimer < 0f)
        {
            abyssJumpCooldownTimer = 0f;
        }
    }

    public void NotifyClimbCompleted()
    {
        abyssJumpCooldownTimer = Mathf.Max(
            0f,
            abyssJumpCooldownAfterClimb
        );

        movementHoldTimer = 0f;
        heldMovementDirection = Vector3.zero;

        Debug.Log(
            $"[AbyssRaycast] Abyss jump disabled for {abyssJumpCooldownAfterClimb:F2} seconds after climb."
        );
    }

    void UpdateMovementHoldTimer()
    {
        Vector3 movementVelocity = walker.GetMovementVelocity();

        movementVelocity = Vector3.ProjectOnPlane(
            movementVelocity,
            walker.transform.up
        );

        if (movementVelocity.magnitude < movementInputThreshold)
        {
            movementHoldTimer = 0f;
            heldMovementDirection = Vector3.zero;
            return;
        }

        Vector3 currentMovementDirection = movementVelocity.normalized;

        if (heldMovementDirection.sqrMagnitude < 0.0001f)
        {
            heldMovementDirection = currentMovementDirection;
            movementHoldTimer = Time.fixedDeltaTime;
            return;
        }

        float directionAlignment = Vector3.Dot(
            heldMovementDirection,
            currentMovementDirection
        );

        if (directionAlignment < movementDirectionThreshold)
        {
            heldMovementDirection = currentMovementDirection;
            movementHoldTimer = Time.fixedDeltaTime;
            return;
        }

        heldMovementDirection = Vector3.Slerp(
            heldMovementDirection,
            currentMovementDirection,
            0.5f
        ).normalized;

        movementHoldTimer += Time.fixedDeltaTime;
    }

    float GetHorizontalSpeed()
    {
        Vector3 velocity = walker.GetVelocity();

        velocity = Vector3.ProjectOnPlane(
            velocity,
            walker.transform.up
        );

        return velocity.magnitude;
    }

    void LaunchParabolicJump()
    {
        float g = walker.gravity;

        if (g <= 0f)
        {
            Debug.LogWarning(
                "[AbyssRaycast] Cannot launch abyss jump because walker.gravity is zero or negative."
            );

            return;
        }

        float vUp = Mathf.Sqrt(
            2f * g * jumpApexHeight
        );

        float airTime = 2f * vUp / g;

        float vForward = jumpForwardDistance / airTime;

        Transform body = transform.parent != null
            ? transform.parent
            : walker.transform;

        Vector3 launchDirection = heldMovementDirection;

        launchDirection = Vector3.ProjectOnPlane(
            launchDirection,
            body.up
        );

        if (launchDirection.sqrMagnitude < 0.0001f)
        {
            launchDirection = Vector3.ProjectOnPlane(
                body.forward,
                body.up
            ).normalized;
        }
        else
        {
            launchDirection.Normalize();
        }

        Vector3 launch =
            body.up * vUp +
            launchDirection * vForward;

        walker.SetMomentum(launch);
    }

    public void PreventJump()
    {
        canJump = false;
        movementHoldTimer = 0f;
        heldMovementDirection = Vector3.zero;
    }

    public void EnableJump()
    {
        canJump = true;
        movementHoldTimer = 0f;
        heldMovementDirection = Vector3.zero;
    }
}