using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantShrink : MonoBehaviour, IReactToMelody
{
    public void React(Melody receivedMelody)
    {
        gameObject.transform.localScale = gameObject.transform.localScale * 0.9f;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
