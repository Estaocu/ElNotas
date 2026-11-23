using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmActivator : MonoBehaviour
{

    private RhythmAudioPrototype rhythm;
    [SerializeField] private CheckpointZone[] affectedZones;
    void Awake()
    {
        rhythm = FindFirstObjectByType<RhythmAudioPrototype>();
    }

    public void ActivateRhythm()
    {
        rhythm.StartRhythm();

        foreach(CheckpointZone zone in affectedZones)
        {
            zone.ToggleZoneRhythm();
        }
    }
}
