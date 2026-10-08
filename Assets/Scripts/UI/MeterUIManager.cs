using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeterUIManager : MonoBehaviour
{
        
    public Color empty;
    public Color sub4;
    public Color sub8;
    public Color full;

    private List<MeterContainer> containers = new List<MeterContainer>();

    void Awake()
    {
        containers = new List<MeterContainer>(GetComponentsInChildren<MeterContainer>(true));
    }

    public void RefreshColors(int currentMeter)
    {
        foreach(MeterContainer cont in containers)
        {
            cont.RecalculateColor(currentMeter);
        }
    }
}
