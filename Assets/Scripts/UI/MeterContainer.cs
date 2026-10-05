using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeterContainer : MonoBehaviour
{
    public int n; //Container number

    [SerializeField] private Image chargeIm;
    [SerializeField] private Material screenSpaceMaterial; // Material que usa el shader personalizado
    private MeterUIManager man;

    void Start()
    {
        man = FindFirstObjectByType<MeterUIManager>();
        chargeIm.color = man.empty;
        chargeIm.material = null; // Inicia con el shader de UI nativo por defecto
    }

    private void ChangeColor(Color wantedColor, bool useOverlay)
    {
        chargeIm.color = wantedColor;

        // Asignar el material especial solo cuando se requiere la textura screen space.
        // Asignar null restaura el renderizado UI por defecto sin overhead.
        chargeIm.material = useOverlay ? screenSpaceMaterial : null;
    }

    public void RecalculateColor(int meterCharge)
    {
        if (n > meterCharge) 
        {
            ChangeColor(man.empty, false); 
            return;
        }

        switch (meterCharge)
        {
            case <= 0:
                ChangeColor(man.empty, false);
                break;

            case < 4:
                ChangeColor(man.sub4, false);
                break;

            case < 8:
                ChangeColor(man.sub8, false);
                break;

            case 8:
                ChangeColor(man.full, true); // Activa el shader en el estado full
                break;
        }
    }
}
