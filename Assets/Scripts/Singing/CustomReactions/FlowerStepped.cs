using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerStepped : MonoBehaviour
{
    public bool canBufferMelody = true;
    [SerializeField] private Singer singer;
    [SerializeField] private Melody melody;


    void OnTriggerEnter(Collider other)
    {
        if (!canBufferMelody || other.gameObject.layer == 8) return; // Layer 8 == Soundwave
        singer.Sing();
        //Buffer your melody for the next desired beat.


    }
}
