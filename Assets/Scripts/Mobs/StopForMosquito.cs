using UnityEngine;
using UnityEngine.Splines;

public class StopForMosquito : MonoBehaviour
{
    public SplineContainer mySpline;

    void OnTriggerEnter(Collider other)
    {
        // Try to find the component in the object or its parents
        NavMeshSplineTraveler mosquito = other.GetComponentInParent<NavMeshSplineTraveler>();

        if (mosquito != null)
        {
            // Verify if it is traveling on the assigned spline
            if (mosquito.splineContainer != mySpline) return;

            mosquito.StopOnPoint();
            Debug.Log("Mosquito found in parent and stopped");
        }
    }
}