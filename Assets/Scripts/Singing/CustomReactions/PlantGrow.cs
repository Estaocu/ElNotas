using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGrow : MonoBehaviour, IReactToMelody
{
    public void React(Melody receivedMelody)
    {
        Debug.Log("Hola bro he recibido el melodio: " + receivedMelody.melodyName);
        gameObject.transform.localScale = gameObject.transform.localScale * 2;
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
