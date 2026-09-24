using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CheckpointZone
{
    public int zoneID;
    public Checkpoint[] points;
    public Checkpoint latestPoint;

    public void SetLatestPoint(Checkpoint point)
    {
        //falta comprobacion de que ese point esté en la lista de points de esta zona
        latestPoint = point;
    }
}

public class CheckpointManager : MonoBehaviour
{
    public CheckpointZone currentZone;
    
    public void SendPlayerToPoint(bool dead)
    {
        if (dead)
        {
            Debug.Log(currentZone.points[0].transform);
        }

        else
        {
            Debug.Log(currentZone.latestPoint.transform);
        }
    }
}
