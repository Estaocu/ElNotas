using System;
using UnityEngine;

public class HostileEntityAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Returning, Asking }

    [Header("Behavior Mode")]
    public bool isStatic = false;

    [Header("Settings")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 5f;
    public float attackDistance = 2f;
    public string[] targetTags = { "Player", "NPC" };

    private State currentState;
    private Transform currentTarget;
    private NavMeshSplineTraveler traveler;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Singer singer;
    private BeatWaitHandle relaxHandle;
    private bool hasAskedForMelody = false;

    void Awake()
    {
        singer = GetComponent<Singer>();
        traveler = GetComponent<NavMeshSplineTraveler>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        currentState = isStatic ? State.Idle : State.Patrol;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Chase:
                HandleChase();
                break;
            case State.Returning:
                HandleReturning();
                break;
            case State.Asking:
                HandleAsking();
                break;
        }
    }

    private void HandleAsking()
    {
        if (currentTarget == null) { Debug.Log("[HostileEntityAI] Target lost while Asking, returning"); StartReturn(); return; }
        traveler.MoveToDestination(currentTarget.position, chaseSpeed);
    }

    private void HandleChase()
    {
        if (currentTarget == null) { Debug.Log("[HostileEntityAI] Target lost while chasing, returning"); StartReturn(); return; }

        traveler.MoveToDestination(currentTarget.position, chaseSpeed);

        if (Vector3.Distance(transform.position, currentTarget.position) <= attackDistance && !hasAskedForMelody)
        {
            AskForMelody();
        }
    }

    private void HandleReturning()
    {
        if (traveler.HasReachedDestination())
        {
            if (isStatic)
            {
                Debug.Log("[HostileEntityAI] Returned to spawn, state = Idle");
                transform.rotation = spawnRotation;
                currentState = State.Idle;
            }
            else
            {
                Debug.Log("[HostileEntityAI] Returned to patrol path, state = Patrol");
                currentState = State.Patrol;
                traveler.ResumePatrol(patrolSpeed);
            }
            hasAskedForMelody = false;
        }
    }

    private void StartReturn()
    {
        if (traveler == null) return;

        currentTarget = null;
        currentState = State.Returning;

        if (isStatic)
        {
            traveler.MoveToDestination(spawnPosition, patrolSpeed);
        }
        else
        {
            if (traveler.splineContainer == null) return;

            float t = traveler.FindClosestPointOnSpline(transform.position);
            Vector3 returnPos = (Vector3)traveler.splineContainer.EvaluatePosition(t);
            traveler.MoveToDestination(returnPos, patrolSpeed);
        }
    }

    private void AskForMelody()
    {
        Debug.Log("[HostileEntityAI] AskForMelody! Singing, waiting for soundwave emission to start timer...");
        hasAskedForMelody = true;
        currentState = State.Asking;
        singer.onSoundwaveSpawned += OnMelodyEmitted;
        singer.Sing();
    }

    private void OnMelodyEmitted(Melody melody)
    {
        singer.onSoundwaveSpawned -= OnMelodyEmitted;
        Debug.Log($"[HostileEntityAI] Soundwave emitted ({melody?.melodyName}), starting 16-subbeat response timer");
        relaxHandle = RhythmBeatWaiter.WaitForSubBeats(16, BeatWaitMode.Immediate, OnRelaxDone);
    }

    public void CalmDown()
    {
        if (currentState != State.Asking) return;
        Debug.Log("[HostileEntityAI] CalmDown! Heard MosquitoRelax, canceling 16-beat timer and returning");
        relaxHandle?.Cancel();
        StartReturn();
    }

    private void OnRelaxDone()
    {
        if (this == null || gameObject == null) return;
        Debug.Log("[HostileEntityAI] OnRelaxDone! 16 beats elapsed, no MosquitoRelax response - ATTACKING!");
        relaxHandle?.Cancel();
        Attack();
        StartReturn();
    }

    private void Attack()
{
    Debug.Log($"[HostileEntityAI] ⚔️ ATTACKING {currentTarget?.name ?? "unknown"}!");
    
    // Obtener el componente LifeAndMeter del objetivo
    LifeAndMeter lifeMeter = currentTarget.GetComponent<LifeAndMeter>();
    
    if (lifeMeter != null)
    {
        // Si el objetivo tiene LifeAndMeter (es el player), infligir daño
        lifeMeter.OnHit(1); // 1 de daño, ajusta según necesites
    }
}

    private void OnTriggerEnter(Collider other)
    {
        if (currentState == State.Chase || currentState == State.Returning || currentState == State.Asking) return;

        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                Debug.Log($"[HostileEntityAI] Detected {other.name}, state = Chase");
                currentTarget = other.transform;
                currentState = State.Chase;
                break;
            }
        }
    }
}
