using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointZone : MonoBehaviour
{
    public int zoneID;
    public Checkpoint[] points;
    public Checkpoint latestPoint;
    private CheckpointManager manager;

    void Start()
    {
        points = GetComponentsInChildren<Checkpoint>(true);
        latestPoint = points[0];

        manager = transform.parent.GetComponent<CheckpointManager>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CountCheckpoints();
    }
#endif

    private void CountCheckpoints()
    {
        points = GetComponentsInChildren<Checkpoint>(true);
    }

    public void SetLatestPoint(Checkpoint point)
    {
        //falta comprobacion de que ese point esté en la lista de points de esta zona
        if (point.Zone == this)
        {
            latestPoint = point;
            manager.currentZone = this;
        }
        
    }

}
