using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshSplineTraveler : MonoBehaviour
{
    public NavMeshAgent agent;
    public SplineContainer splineContainer;
    
    [Header("Movement Settings")]
    public float arrivalDistance = 0.5f;
    public bool isLoop = true; 

    private float splineT = 0f;
    private bool isPatrolling = false;
    private bool movingForward = true;

    void Start()
    {
        HostileEntityAI ai = GetComponent<HostileEntityAI>();
        if (ai != null)
        {
            isPatrolling = !ai.isStatic;
            agent.speed = ai.patrolSpeed;
        }
    }

    void Update()
    {
        if (isPatrolling && splineContainer != null) 
            MoveAlongSpline();
    }

    private void MoveAlongSpline()
    {
        // 1. Calculamos cuánto debería avanzar T basándonos en la velocidad del agente
        // La longitud de la spline nos ayuda a que el avance sea constante en metros
        float splineLength = splineContainer.CalculateLength();
        float speedInT = agent.speed / splineLength; 
        float delta = speedInT * Time.deltaTime;

        // 2. Avanzamos T según el modo (Loop o Ping-Pong)
        if (isLoop)
        {
            splineT = (splineT + delta) % 1f;
        }
        else
        {
            if (movingForward)
            {
                splineT += delta;
                if (splineT >= 1f) { splineT = 1f; movingForward = false; }
            }
            else
            {
                splineT -= delta;
                if (splineT <= 0f) { splineT = 0f; movingForward = true; }
            }
        }

        // 3. Colocamos el destino del NavMesh un poco por delante del agente en la curva
        // Esto crea un "efecto imán" que obliga al agente a seguir la forma de la spline
        Vector3 targetPos = (Vector3)splineContainer.EvaluatePosition(splineT);
        agent.SetDestination(targetPos);
    }

    public void MoveToDestination(Vector3 destination, float speed)
    {
        isPatrolling = false;
        agent.speed = speed;
        agent.SetDestination(destination);
    }

    public void ResumePatrol(float speed)
    {
        agent.speed = speed;
        splineT = FindClosestPointOnSpline(transform.position);
        movingForward = (splineT < 0.99f); 
        isPatrolling = true;
    }

    public bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= arrivalDistance;
    }

    public float FindClosestPointOnSpline(Vector3 worldPosition)
    {
        if (splineContainer == null) return 0f;
        float3 localPos = splineContainer.transform.InverseTransformPoint(worldPosition);
        SplineUtility.GetNearestPoint(splineContainer.Spline, localPos, out _, out float t);
        return t;
    }
}
