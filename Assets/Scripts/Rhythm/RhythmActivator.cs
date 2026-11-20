using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmActivator : MonoBehaviour
{

    private RhythmAudioPrototype rhythm;
    void Awake()
    {
        rhythm = FindFirstObjectByType<RhythmAudioPrototype>();
    }

    public void ActivateRhythm()
    {
        rhythm.StartRhythm();
    }
}
