using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MosquitoAI : MonoBehaviour, IReactToMelody
{
    public enum State {Idle, Patrol, Chase, Return, Stunned}
    private State currentState;
    public bool isStatic = false;
    [Header("Settings")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 5f;
    public float stoppingDistance = 2f;
    public int stunDuration = 32;
    private bool hasAskedForMelody = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private int peaceTime = 64;
    private bool peaceful = false;


    
    public string[] targetTags = { "Player", "NPC" };
    private NavMeshSplineTraveler traveler;
    private Singer singer;
    private GameObject currentTarget;
    private BeatWaitHandle relaxHandle;
    private BeatWaitHandle stunHandle;


    

    void Awake()
    {
        traveler = GetComponent<NavMeshSplineTraveler>();
        currentState = isStatic ? State.Idle : State.Patrol;
        singer = GetComponent<Singer>();
    }

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (!isStatic && currentState == State.Patrol && traveler != null)
        {
            traveler.ResumePatrol(patrolSpeed);
        }
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
        if (currentState == State.Chase || currentState == State.Return || currentState == State.Stunned || peaceful) return;
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                Debug.Log($"Chasing {other.name}");
                currentTarget = other.gameObject;
                currentState = State.Chase;
                break;
            }
        }
    }

    private void HandleChase()
    {
        if (currentTarget == null) return;

        Vector3 offset = currentTarget.transform.position - transform.position;
    
        if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance || !hasAskedForMelody)
        { AskForMelody(); return; }

        Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
        Vector3 stoppingPoint = currentTarget.transform.position - directionToTarget * stoppingDistance;
        traveler.MoveToDestination(stoppingPoint, chaseSpeed);    
    }
    private void AskForMelody()
    {
        if (hasAskedForMelody) return;
        singer.onSoundwaveSpawned += OnMelodyEmitted; //Avisame cuando la soundwave se spawnee 
        singer.Sing(); //Empieza a meter notas en la cola y ve cantando
        hasAskedForMelody = true;

    }

    private void OnMelodyEmitted(Melody melody)
{
    singer.onSoundwaveSpawned -= OnMelodyEmitted;
    
    if (currentState == State.Stunned) return;
    
    relaxHandle = RhythmBeatWaiter.WaitForSubBeats(32, BeatWaitMode.Immediate, NoRelaxReceived);
}

    private void NoRelaxReceived()
    {
        relaxHandle?.Cancel();
        if (currentState == State.Stunned || peaceful) return;
        if (currentTarget == null) 
        {
        StartReturn();
        return;
        } 
        
        Attack();
        StartReturn();
    }
    private void Attack()
    {
        if (peaceful || currentState == State.Stunned) return;
        //Animación de atacar
        LifeAndMeter playerHp = currentTarget.GetComponent<LifeAndMeter>();
        if (playerHp != null) playerHp.OnHit(1);

        peaceful = true;
        RhythmBeatWaiter.WaitForSubBeats(peaceTime, BeatWaitMode.Immediate, () => peaceful = false);
 
        
    }
    private void StartReturn()
    {
        if (traveler == null) return;

        currentTarget = null;
        currentState = State.Return;

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

    public void React(Melody receivedMelody)
    {
        if (currentState != State.Chase) return;

        relaxHandle?.Cancel();
        singer.StopSinging();

        currentState = State.Stunned;
        traveler.MoveToDestination(transform.position, patrolSpeed);

        Debug.Log("MOSQUITO RELAXED");
        peaceful = true;
        RhythmBeatWaiter.WaitForSubBeats(stunDuration, BeatWaitMode.Immediate, StartReturn);
        RhythmBeatWaiter.WaitForSubBeats(peaceTime, BeatWaitMode.Immediate, () => peaceful = false);
        
    }
    }

