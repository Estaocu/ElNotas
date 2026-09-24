using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Meter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI meterText;

    
    public int currentMeter;
    public int maxMeter = 8;

    public bool hasFullMeter => currentMeter >= maxMeter;

    void Start()
    {
        if (meterText != null)
            meterText.SetText(currentMeter.ToString());
    }

    private void SetMeter(int wantedMeter)
    {
        currentMeter = wantedMeter;

        if (meterText != null)
            meterText.SetText(currentMeter.ToString());
    }

    public void ConsumeFullMeter()
    {
        SetMeter(0);
    }

    public void DebugMaxMeter(InputAction.CallbackContext context)
    {
        if (context.performed) SetMeter(maxMeter);
        if (meterText != null) meterText.SetText(currentMeter.ToString());
        Debug.Log("Meter Maxed");
    }

    public void IncreaseByOne()
    {
        SetMeter(Mathf.Clamp(currentMeter + 1, 0, maxMeter));
    }
}
