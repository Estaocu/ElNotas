using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class MosquitoBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public SplineContainer splineContainer;

    [Header("Patrol Settings")]
    public float patrolSpeed = 3f;
    public float splineMoveSpeed = 0.2f; // avance sobre spline (0–1 por segundo)

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float attackDistance = 2f;

    private Transform currentTarget;
    private bool hasTarget = false;

    private float splineT = 0f; // posición normalizada en spline (0–1)

    private enum State { Patrol, Chase }
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
        }
    }

    void Patrol()
    {
        if (splineContainer == null) return;

        // avanzar por spline
        splineT += splineMoveSpeed * Time.deltaTime;
        if (splineT > 1f) splineT -= 1f;

        // obtener posición en spline
        Vector3 splinePos = splineContainer.EvaluatePosition(splineT);

        agent.SetDestination(splinePos);
    }

    void Chase()
    {
        if (currentTarget == null)
        {
            ReturnToSpline();
            return;
        }

        agent.SetDestination(currentTarget.position);

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (dist <= attackDistance)
        {
            Attack();
            ReturnToSpline();
        }
    }

    void Attack()
    {
        // Placeholder para futura implementación
        Debug.Log("Mosquito attacks target");
    }

    void ReturnToSpline()
    {
        hasTarget = false;
        currentTarget = null;

        currentState = State.Patrol;
        agent.speed = patrolSpeed;

        // encontrar punto más cercano en spline
        splineT = FindClosestPointOnSpline(transform.position);
    }

    float FindClosestPointOnSpline(Vector3 position)
    {
        int resolution = 50; // más alto = más preciso
        float closestT = 0f;
        float minDist = float.MaxValue;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 point = splineContainer.EvaluatePosition(t);

            float dist = Vector3.Distance(position, point);
            if (dist < minDist)
            {
                minDist = dist;
                closestT = t;
            }
        }

        return closestT;
    }

    // --- TRIGGER DETECTION (desde hijo collider) ---
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