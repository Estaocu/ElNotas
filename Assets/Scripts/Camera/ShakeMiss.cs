using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraShake;

public class ShakeMiss : MonoBehaviour
{
    [SerializeField] private PlayerRhythmController instrument;
    [SerializeField] private float strength = 0.5f;
    [SerializeField] private float freq = 35;
    [SerializeField] private int bounces = 6;

    private void OnEnable()
    {
        if (instrument != null)
        {
            instrument.OnNoteRejected += MissShake;
        }
    }

    private void OnDisable()
    {
        if (instrument != null)
        {
            instrument.OnNoteRejected -= MissShake;

        }
    }
    public void MissShake()
    {
        CameraShaker.Presets.ShortShake3D(strength, freq, bounces);
    }
}
