using UnityEngine;
using CMF;

[ExecuteInEditMode]
public class AbyssRaycast : MonoBehaviour
{
    public Vector3 ray1Start;
    public Vector3 ray2Start;
    public float rayLength;
    private bool canJump = true;
    [SerializeField] private float minJumpSpeed;

    [SerializeField] private Mover mover;
    [SerializeField] private AdvancedWalkerController walker;

    [Tooltip("Layers considered ground. Exclude Player, Feet and AbyssRaycast so the rays ignore the player's own colliders.")]
    [SerializeField] private LayerMask groundMask = ~0;

    public float jumpApexHeight = 2f;
    public float jumpForwardDistance = 4f;

    void OnEnable()
    {
        if (Application.isPlaying && walker != null)
            walker.OnLand += HandleLand;
    }

    void OnDisable()
    {
        if (walker != null)
            walker.OnLand -= HandleLand;
    }

    // Re-arm the auto-jump once the controller regains ground contact;
    void HandleLand(Vector3 collisionVelocity)
    {
        canJump = true;
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
        bool hitSomething = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore);

        Gizmos.color = color;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayLength);

        if (hitSomething)
        {
            Gizmos.DrawSphere(hit.point, 0.05f);
        }
    }

    void FixedUpdate()
    {
        if (transform.parent == null)
        {
            return;
        }

        Vector3 worldRay1Start = transform.parent.TransformPoint(ray1Start);
        Vector3 worldRay2Start = transform.parent.TransformPoint(ray2Start);

        bool ray1DetectsGround = Physics.Raycast(worldRay1Start, Vector3.down, rayLength, groundMask, QueryTriggerInteraction.Ignore);
        bool ray2DetectsGround = Physics.Raycast(worldRay2Start, Vector3.down, rayLength, groundMask, QueryTriggerInteraction.Ignore);

        if (ray1DetectsGround)
        {
            Debug.DrawRay(worldRay1Start, Vector3.down * rayLength, Color.magenta);
        }

        if (ray2DetectsGround)
        {
            Debug.DrawRay(worldRay2Start, Vector3.down * rayLength, Color.blue);
        }

        bool bothRaysDetectAbyss = !ray1DetectsGround && !ray2DetectsGround;

        if (bothRaysDetectAbyss && mover.IsGrounded() && canJump)
            {
                float currentSpeed = walker.GetVelocity().magnitude;
                if (currentSpeed >= minJumpSpeed)
                {
                    LaunchParabolicJump();
                    canJump = false;
                }
                else
                {
                    Debug.Log("Fall and grab ledge");
                    canJump = false;
                }
            }
    }

    void LaunchParabolicJump()
{
    float g = walker.gravity;                       // usa la misma gravedad del controlador
    float vUp = Mathf.Sqrt(2f * g * jumpApexHeight);
    float airTime = 2f * vUp / g;
    float vForward = jumpForwardDistance / airTime;

    Transform body = transform.parent;              // el PLAYER
    Vector3 launch = body.up * vUp + body.forward * vForward;
    walker.SetMomentum(launch);
}
}