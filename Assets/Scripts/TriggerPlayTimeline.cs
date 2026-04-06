using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables; //Epa, esto es custom de aqui!!!!

public class TriggerPlayTimeline : MonoBehaviour
{

    [SerializeField] private PlayableDirector tlDirector;
    [SerializeField] private bool onlyOnce = true;
    private bool alreadyPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (onlyOnce && alreadyPlayed) return;
            if (tlDirector != null)
            {
                tlDirector.Play();
                alreadyPlayed = true;
                gameObject.SetActive(false);
            }
        }
    }
}
