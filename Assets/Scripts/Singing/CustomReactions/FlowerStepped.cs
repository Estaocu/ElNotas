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
        if (canBufferMelody == false || other.layerOverridePriority == 8) return;

    }
}
