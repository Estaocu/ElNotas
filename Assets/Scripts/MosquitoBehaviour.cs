using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using Unity.Mathematics;

public class MosquitoBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public SplineContainer splineContainer;

    [Header("Patrol Settings")]
    public float patrolSpeed = 3f;
    public float splineMoveSpeed = 0.2f;
    public float arrivalDistance = 0.5f;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float attackDistance = 2f;

    private Transform currentTarget;
    private bool hasTarget = false;
    private float splineT = 0f;

    private enum State { Patrol, Chase, Returning }
    private State currentState = State.Patrol;

    void Start()
    {
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = patrolSpeed;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Returning:
                Returning();
                break;
        }
    }

    void Patrol()
    {
        if (splineContainer == null) return;

        splineT += splineMoveSpeed * Time.deltaTime;
        if (splineT > 1f) splineT -= 1f;

        Vector3 splinePos = (Vector3)splineContainer.EvaluatePosition(splineT);
        agent.SetDestination(splinePos);
    }

    void Chase()
    {
        if (currentTarget == null)
        {
            StartReturn();
            return;
        }

        agent.SetDestination(currentTarget.position);

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (dist <= attackDistance)
        {
            Attack();
            StartReturn();
        }
    }

    void Returning()
    {
        Vector3 targetSplinePos = (Vector3)splineContainer.EvaluatePosition(splineT);
        agent.SetDestination(targetSplinePos);

        if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
        {
            agent.ResetPath();
            currentState = State.Patrol;
        }
    }

    void Attack()
    {
        Debug.Log("Mosquito attacks target");
    }

    void StartReturn()
    {
        hasTarget = false;
        currentTarget = null;
        agent.speed = patrolSpeed;

        // ahora sí: punto REAL más cercano en la curva
        splineT = FindClosestPointOnSpline(transform.position);

        currentState = State.Returning;
    }

    float FindClosestPointOnSpline(Vector3 worldPosition)
{
    // Convertimos la posición del mundo al espacio local del SplineContainer
    float3 localPos = splineContainer.transform.InverseTransformPoint(worldPosition);

    // Buscamos el punto más cercano en el espacio local
    // El tercer parámetro es el 't' (0 a 1) que devuelve la función
    SplineUtility.GetNearestPoint(
        splineContainer.Spline, 
        localPos, 
        out float3 nearestLocalPos, 
        out float t
    );

    return t;
}

    private void OnTriggerEnter(Collider other)
    {
        if (hasTarget) return;

        if (other.CompareTag("Player") || other.CompareTag("NPC"))
        {
            currentTarget = other.transform;
            hasTarget = true;

            currentState = State.Chase;
            agent.speed = chaseSpeed;
        }
    }
}