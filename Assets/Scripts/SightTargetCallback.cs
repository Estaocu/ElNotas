using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Devdog.LosPro;

public class SightTargetCallback : MonoBehaviour, ISightTargetCallbacks
{
    void ISightTargetCallbacks.OnCameIntoObserverRange(SightTargetInfo sightInfo)
    {

    }

    void ISightTargetCallbacks.OnDetectedByObserver(SightTargetInfo sightInfo)
    {
    }

    void ISightTargetCallbacks.OnGettingDetected(SightTargetInfo sightInfo)
    {
    }

    void ISightTargetCallbacks.OnObserverTryingToDetect(SightTargetInfo sightInfo)
    {
    }

    void ISightTargetCallbacks.OnStopGettingDetected(SightTargetInfo sightInfo)
    {
    }

    void ISightTargetCallbacks.OnUnDetectedByObserver(SightTargetInfo sightInfo)
    {
    }

    void ISightTargetCallbacks.OnWentOutOffObserverRange(SightTargetInfo sightInfo)
    {
    }
}
