using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MosquitoAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Return, Stunned }
    public State currentState;

    public bool isStatic = false;

    [Header("Settings")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 5f;
    public float stoppingDistance = 2f;
    public int stunDuration = 32;
    [SerializeField] private int timeTilBite = 4;

    private bool hasAskedForMelody = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    [SerializeField] private int peaceTime = 64;
    private bool peaceful = false;

    public string[] targetTags = { "Player", "NPC" };

    private NavMeshSplineTraveler traveler;
    private Singer singer;
    private AddNotesOnBeat beatSinger;
    private RhythmClock rhythmClock;

    private GameObject currentTarget;

    private Coroutine relaxCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine peaceCoroutine;

    [SerializeField] private SingleNotesListener listener;
    [SerializeField] private notesEnum[] relaxMelody;

    public bool wantsAnswer;

    [Header("Raycast Confirmation Settings")]
    [SerializeField] private Transform raycastStartPoint;
    [SerializeField] private LayerMask obstacleLayerMask;

    private bool getTargetNow = false;

    void Awake()
    {
        traveler = GetComponent<NavMeshSplineTraveler>();
        currentState = isStatic ? State.Idle : State.Patrol;
        singer = GetComponent<Singer>();
        beatSinger = GetComponent<AddNotesOnBeat>();

        // There is only one RhythmClock in the scene.
        rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (!isStatic &&
            currentState == State.Patrol &&
            traveler != null)
        {
            traveler.ResumePatrol(patrolSpeed);
        }

        List<notesEnum[]> melodies = new List<notesEnum[]>
        {
            relaxMelody
        };

        listener.SetDesiredMelodies(melodies);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Chase:
                HandleChase();
                break;

            case State.Return:
                HandleReturn();
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentState == State.Chase ||
            currentState == State.Return ||
            currentState == State.Stunned ||
            peaceful)
        {
            return;
        }

        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                currentTarget = other.gameObject;
                RayToTarget(other);
                if(!getTargetNow) return;
                currentState = State.Chase;
                Debug.Log($"Chasing {other.name}");

                break;
            }
        }

        

        
    }

    private void HandleChase()
    {
        if (currentTarget == null)
            return;

        Vector3 offset =
            currentTarget.transform.position -
            transform.position;

        if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance || !hasAskedForMelody)
        {
            AskForMelody();
            //Debug.Log("Started asking for melody");
            hasAskedForMelody = true;
            return;
        }

        Vector3 directionToTarget =
            (currentTarget.transform.position -
             transform.position).normalized;

        Vector3 stoppingPoint =
            currentTarget.transform.position -
            directionToTarget * stoppingDistance;

        traveler.MoveToDestination(
            stoppingPoint,
            chaseSpeed
        );
    }

    private void AskForMelody()
    {
        if (hasAskedForMelody) return;

        if (beatSinger != null)
            beatSinger.Sing();
    }

    public void OnQuestionDone()
    {
        if (currentState == State.Stunned) return;

        wantsAnswer = true;

        Debug.Log("Mosquito started angry timer. Looking for melody");

        CancelRelaxWait();

        relaxCoroutine = StartCoroutine(RelaxWaitRoutine());
    }

    private IEnumerator RelaxWaitRoutine()
    {
        yield return rhythmClock.WaitForSubBeats(timeTilBite);

        relaxCoroutine = null;

        NoRelaxReceived();
    }

    private void NoRelaxReceived()
    {
        if (currentState == State.Stunned || peaceful) return;

        if (currentTarget == null)
        {
            StartReturn();
            wantsAnswer = false;
            return;
        }

        Attack();
        StartReturn();
        wantsAnswer = false;
    }

    private void Attack()
    {
        if (peaceful ||currentState == State.Stunned) return;

        // Attack animation.

        PlayerHP hp = currentTarget.GetComponent<PlayerHP>();

        if (hp != null) hp.OnHit();

        peaceful = true;

        CancelPeaceWait();

        peaceCoroutine = StartCoroutine(PeaceWaitRoutine());
    }

    private IEnumerator PeaceWaitRoutine()
    {
        yield return rhythmClock.WaitForSubBeats(peaceTime);

        peaceCoroutine = null;

        peaceful = false;
    }

    private void StartReturn()
    {
        wantsAnswer = false;
        if (traveler == null) return;

        currentTarget = null;
        currentState = State.Return;

        if (isStatic)
        {
            traveler.MoveToDestination(spawnPosition,patrolSpeed);
        }
        else
        {
            if (traveler.splineContainer == null) return;

            float t = traveler.FindClosestPointOnSpline(transform.position);

            Vector3 returnPos =(Vector3)traveler.splineContainer.EvaluatePosition(t);

            traveler.MoveToDestination(returnPos,patrolSpeed);
        }
    }

    private void HandleReturn()
    {
        if (traveler.HasReachedDestination())
        {
            if (isStatic)
            {
                transform.rotation = spawnRotation;
                currentState = State.Idle;
            }
            else
            {
                currentState = State.Patrol;
                traveler.ResumePatrol(patrolSpeed);
            }

            hasAskedForMelody = false;
        }
    }

    public void BeRelaxed()
    {
        if (currentState != State.Chase || !hasAskedForMelody) return;

        if (!wantsAnswer) return;
        
        CancelRelaxWait();

        if (beatSinger != null) beatSinger.StopSinging();

        currentState = State.Stunned;

        traveler.MoveToDestination(transform.position,patrolSpeed);

        Debug.Log("MOSQUITO RELAXED");

        peaceful = true;

        CancelStunWait();

        stunCoroutine = StartCoroutine(StunWaitRoutine());

        CancelPeaceWait();

        peaceCoroutine =StartCoroutine(PeaceWaitRoutine());
        
    }

    private IEnumerator StunWaitRoutine()
    {
        yield return rhythmClock.WaitForSubBeats(stunDuration);

        stunCoroutine = null;

        StartReturn();
    }

    private void CancelRelaxWait()
    {
        if (relaxCoroutine != null)
        {
            StopCoroutine(relaxCoroutine);
            relaxCoroutine = null;
        }
    }

    private void CancelStunWait()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
    }

    private void CancelPeaceWait()
    {
        if (peaceCoroutine != null)
        {
            StopCoroutine(peaceCoroutine);
            peaceCoroutine = null;
        }
    }

    private void RayToTarget(Collider playerCol)
    {
            Vector3 startPos =
                raycastStartPoint != null
                    ? raycastStartPoint.position
                    : transform.position;

            Vector3 targetPos = playerCol.bounds.center;

            Vector3 direction = targetPos - startPos;

            float distance = direction.magnitude;

            if (Physics.Raycast(
                    startPos,
                    direction.normalized,
                    out RaycastHit hit,
                    distance,
                    obstacleLayerMask))
            {
                if (hit.collider.transform.root != playerCol.transform.root)
                {

                    Debug.DrawLine(
                        startPos,
                        playerCol.bounds.center,
                        Color.red
                    );

                    getTargetNow = false;
                    return;
                }
            }
            else
            {
                Debug.DrawLine(
                    startPos,
                    playerCol.bounds.center,
                    Color.green
                );

                getTargetNow = true;
            }
        }

        
    }