using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mode { Forward, Homing }
public class HomingProjectile : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    public Transform latestNoteTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float explodeTime = 5f;
    [SerializeField] private int speedIncreaseRatio = 20; 
    [SerializeField] private float impactThreshold = 5f;
    
    public GameObject sender;
    public GameObject target;
    public bool movingForward = false;
    public bool followingTarget = false;

    public bool destinationAlreadySet = false;
    private float lastSqrDistance = float.MaxValue;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (movingForward)
        {
            MoveForward();
            return;
        }

        if (followingTarget && target != null)
        {
            MoveTowardsTarget();
            CheckImpact();
        }
        else if (followingTarget && target == null)
        {
            // Si perdemos el objetivo, volvemos a modo Forward para no quedarnos quietos
            SetMovementMode(Mode.Forward, sender);
        }
    }

    public void MoveForward()
    {
        if (destinationAlreadySet) return;
        moveDirection = (gameObject.transform.position - latestNoteTransform.position).normalized;
        rb.velocity = moveDirection * speed;
        
        // Face movement direction, keeping original X rotation if needed
        float currentX = transform.localEulerAngles.x;
        gameObject.transform.forward = moveDirection;
        transform.localEulerAngles = new Vector3(currentX, transform.localEulerAngles.y, transform.localEulerAngles.z);
        
        destinationAlreadySet = true;
    }

    public void MoveTowardsTarget()
    {
        if (target == null) return;

        // Correct movement direction: FROM projectile TO target
        moveDirection = (target.transform.position - transform.position).normalized;
        rb.velocity = moveDirection * speed;

        // Fix: Point FORWARD towards the target (was pointing away)
        float currentX = transform.localEulerAngles.x;
        gameObject.transform.forward = moveDirection; 
        transform.localEulerAngles = new Vector3(currentX, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }

    public void SaveNotePosition(Transform noteTransform)
    {
        latestNoteTransform = noteTransform;
    }

    public void SetMovementMode(Mode mode, GameObject remitente)
    {
        sender = remitente;
        destinationAlreadySet = false;
        lastSqrDistance = float.MaxValue; // Reset tracking

        switch (mode)
        {
            case Mode.Forward:
                ResetRBVelocity();
                followingTarget = false;
                movingForward = true;
                break;

            case Mode.Homing:
                ResetRBVelocity();
                movingForward = false;
                followingTarget = true;
                break;
        }
    }

    public void ResetRBVelocity()
    {
        if (rb != null) rb.velocity = Vector3.zero;
    }

    public void LookAtNewTarget(Transform where)
    {
        // Placeholder for compatibility
    }

    public void CheckImpact()
    {
        if (target == null) return;

        Vector3 toTarget = target.transform.position - transform.position;
        float currentSqrDistance = toTarget.sqrMagnitude;
        
        // IMPACT DETECTION:
        // 1. Direct proximity
        bool isInside = currentSqrDistance < (impactThreshold * impactThreshold);
        
        // 2. Pass-through detection (Dot Product):
        // If the target is now "behind" our movement direction, we overshot it this frame.
        bool hasOvershot = Vector3.Dot(moveDirection, toTarget) < 0;

        if (isInside || (hasOvershot && currentSqrDistance < impactThreshold * impactThreshold * 4f))
        {
            Explode();
        }

        lastSqrDistance = currentSqrDistance;
    }

    public void Explode()
    {
        GameObject explosionPrefab = Resources.Load<GameObject>("ExplosionDecalPrefab");
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("ExplosionDecalPrefab not found in Resources! Did you run Tools > El Notas > Create Explosion Prefab?");
        }

        Destroy(gameObject);
    }
}
