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
    public float decelerationRate = 2f;

    private float splineT = 0f;
    private bool isPatrolling = false;
    private bool movingForward = true;

    public float originalSpeed;
    private bool isDecelerating = false;


    void Start()
    {
        HostileEntityAI ai = GetComponent<HostileEntityAI>();
        if (ai != null)
        {
            isPatrolling = !ai.isStatic;
            agent.speed = ai.patrolSpeed;
            originalSpeed = ai.patrolSpeed;
        }
        else
        {
            originalSpeed = agent.speed;
        }
    }

    void Update()
    {

        if (isDecelerating)
        {
            PerformDeceleration();
        }

        if (isPatrolling && splineContainer != null) 
            MoveAlongSpline();
    }

    private void MoveAlongSpline()
    {
        float splineLength = splineContainer.CalculateLength();
        float speedInT = agent.speed / splineLength; 
        float delta = speedInT * Time.deltaTime;

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

        Vector3 targetPos = (Vector3)splineContainer.EvaluatePosition(splineT);
        agent.SetDestination(targetPos);
    }

    private void PerformDeceleration()
    {
        // Gradually reduce speed to 0
        agent.speed = Mathf.MoveTowards(agent.speed, 0, decelerationRate * Time.deltaTime);

        if (agent.speed <= 0.01f)
        {
            agent.speed = 0;
            isDecelerating = false;
            
            Debug.Log("Hemos llamado al reloj, pronto empezamos a patrullar de nuevo");
            RhythmBeatWaiter.WaitForSubBeats(24, BeatWaitMode.Immediate, () => ResumePatrol(originalSpeed));
        }
    }

    public void MoveToDestination(Vector3 destination, float speed)
    {
        isPatrolling = false;
        isDecelerating = false;
        agent.speed = speed;
        agent.SetDestination(destination);
    }

    public void ResumePatrol(float speed)
    {
        agent.speed = speed;
        splineT = FindClosestPointOnSpline(transform.position);
        movingForward = (splineT < 0.99f); 
        isPatrolling = true;
        isDecelerating = false;
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

    public void StopOnPoint()
    {
        if (!isPatrolling) return;
        isDecelerating = true;

    }

}

