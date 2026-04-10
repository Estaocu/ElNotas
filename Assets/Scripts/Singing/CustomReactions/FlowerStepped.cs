using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerStepped : MonoBehaviour
{
    [SerializeField] private Singer singer;
    [SerializeField] private Melody melody;


    void OnTriggerEnter(Collider other)
    {
        singer.SpawnSoundwave(melody);
    }
}
