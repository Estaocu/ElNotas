using CMF;
using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerHP : MonoBehaviour
{
    // Campos privados (Estado interno)
    private int currentHp;
    private float currentMeter => meter.currentMeter;

    // Propiedades calculadas (Solo lectura para el exterior)
    public bool isMeterFull => meter.hasFullMeter;
    public int GetCurrentHp() => currentHp;

    [SerializeField] private Meter meter;


    void Awake()
    {
        meter = GetComponent<Meter>();
    }

    public void OnHit(int dmg)
    {
        Debug.Log("hit XD");

        if(meter.currentMeter >= 4)
        {
            meter.ConsumeFullMeter();
            Debug.Log("TP to closest spawnpoint");
        }

        else
        {
            meter.ConsumeFullMeter();
            Debug.Log("GAME OVER. TP to first spawnpoint in this zone");

        }
    }
}