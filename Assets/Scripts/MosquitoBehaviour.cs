using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Devdog.LosPro;

public class MosquitoBehaviour : MonoBehaviour, IObserverCallbacks
{
    public GameObject target; 
    void IObserverCallbacks.OnDetectedTarget(SightTargetInfo info)
    {
        
    }

    void IObserverCallbacks.OnDetectingTarget(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnStopDetectingTarget(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnTargetCameIntoRange(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnTargetDestroyed(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnTargetWentOutOfRange(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnTryingToDetectTarget(SightTargetInfo info)
    {
    }

    void IObserverCallbacks.OnUnDetectedTarget(SightTargetInfo info)
    {
    }
}