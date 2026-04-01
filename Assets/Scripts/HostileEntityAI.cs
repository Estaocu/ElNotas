using UnityEngine;

public class HostileEntityAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Returning }
    
    [Header("Behavior Mode")]
    public bool isStatic = false; // Define si patrulla o se queda quieto

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

    void Awake()
    {
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
        }
    }

    private void HandleChase()
    {
        if (currentTarget == null) { StartReturn(); return; }

        traveler.MoveToDestination(currentTarget.position, chaseSpeed);

        if (Vector3.Distance(transform.position, currentTarget.position) <= attackDistance)
        {
            Attack();
            StartReturn();
        }
    }

    private void HandleReturning()
    {
        if (traveler.HasReachedDestination())
        {
            if (isStatic)
            {
                transform.rotation = spawnRotation; // Recupera rotación original
                currentState = State.Idle;
            }
            else
            {
                currentState = State.Patrol;
                traveler.ResumePatrol(patrolSpeed);
            }
        }
    }

    private void StartReturn()
    {
        currentTarget = null;
        currentState = State.Returning;

        if (isStatic)
        {
            traveler.MoveToDestination(spawnPosition, patrolSpeed);
        }
        else
        {
            // Regresa al punto más cercano de la curva
            float t = traveler.FindClosestPointOnSpline(transform.position);
            Vector3 returnPos = (Vector3)traveler.splineContainer.EvaluatePosition(t);
            traveler.MoveToDestination(returnPos, patrolSpeed);
        }
    }

    private void Attack() => Debug.Log(gameObject.name + " atacó!");

    private void OnTriggerEnter(Collider other)
    {
        // Solo busca objetivo si está en Idle o Patrullando
        if (currentState == State.Chase || currentState == State.Returning) return;

        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                currentTarget = other.transform;
                currentState = State.Chase;
                break;
            }
        }
    }
}
