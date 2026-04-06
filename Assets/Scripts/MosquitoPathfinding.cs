using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

[RequireComponent(typeof(NavMeshAgent))]
public class MosquitoPathfinding : MonoBehaviour
{
    [Header("Patrol Settings")]
    public SplineContainer patrolSpline;
    public float speed = 3.5f;
    public float arrivalDistance = 1.0f;

    [Header("LOS Pro detection")]
    public Transform target; // El objetivo detectado por LOSPro
    public bool isPlayerDetected = false;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private int totalWaypoints = 10; // Dividiremos la curva en 10 puntos

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        
        if (patrolSpline != null)
            GoToNextWaypoint();
    }

    void Update()
    {
        if (isPlayerDetected && target != null)
        {
            // MODO PERSECUCIÓN
            agent.SetDestination(target.position);
        }
        else
        {
            // MODO PATRULLA (Seguir la curva)
            if (!agent.pathPending && agent.remainingDistance < arrivalDistance)
            {
                GoToNextWaypoint();
            }
        }
    }

    void GoToNextWaypoint()
    {
        if (patrolSpline == null) return;

        // Calculamos la posición en la curva basada en un ratio (0 a 1)
        float t = (float)currentWaypointIndex / totalWaypoints;
        Vector3 nextPos = patrolSpline.EvaluatePosition(t);

        agent.SetDestination(nextPos);

        // Ciclar el índice para que la patrulla sea infinita
        currentWaypointIndex = (currentWaypointIndex + 1) % (totalWaypoints + 1);
    }

    // Método que llamarías desde tus eventos de LOSPro
    public void OnTargetDetected(Transform newTarget)
    {
        target = newTarget;
        isPlayerDetected = true;
    }

    public void OnTargetLost()
    {
        isPlayerDetected = false;
        target = null;
        // Al perderlo, volverá automáticamente al siguiente punto de la curva
    }
}