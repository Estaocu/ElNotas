using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IReactToMelody))]
public class MelodyListener : MonoBehaviour
{
    public Melody desiredMelody;

    public void OnTriggerEnter(Collider other)
    {
        var soundwave = other.GetComponent<Soundwave>();
        if (!soundwave) return;

        if(desiredMelody != null)
        {
            if (desiredMelody == soundwave.myMelody)
                GetComponent<IReactToMelody>().React(soundwave.myMelody);
            return;
        }

        GetComponent<IReactToMelody>().React(soundwave.myMelody);



    }
}
