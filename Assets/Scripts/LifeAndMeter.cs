using CMF;
using UnityEngine;
using TMPro;
using System.Collections;

public class LifeAndMeter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private int maxMeter = 10;
    [SerializeField] private float stunTime = 2f;
    [SerializeField] private float immunityTime = 2f;
    [SerializeField] public TMP_Text meterText;

    // Campos privados (Estado interno)
    private int currentHp;
    private int currentMeter;
    private bool damageImmune;
    private bool isStunned;

    // Propiedades calculadas (Solo lectura para el exterior)
    public bool IsMeterFull => currentMeter >= maxMeter;
    public int GetCurrentHp() => currentHp;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void OnHit(int dmg)
    {
        if (damageImmune) return;

        if (IsMeterFull)
        {
            DoParry();
        }
        else
        {
            if(currentMeter != 0)
            {
                StartCoroutine(StunRoutine());
                SetExactMeterCharge(0);
                return;
            }

            currentHp = Mathf.Max(currentHp - dmg, 0);
            SetExactMeterCharge(0);
            Debug.Log($"HITS LEFT: {currentHp} / {maxHp}");
            CheckHp();
            
        }
    }

    public void ChangeMeterCharge(int q)
    {
        currentMeter = Mathf.Clamp(currentMeter + q, 0, maxMeter);
        // Debug.Log($"meter charge: {currentMeter} / {maxMeter}");
        meterText.SetText($"Meter: {currentMeter} / {maxMeter}");
    }

    public void SetExactMeterCharge(int q)
    {
        currentMeter = Mathf.Clamp(q, 0, maxMeter);
        meterText.SetText($"Meter: {currentMeter} / {maxMeter}");
    }

    private void DoParry()
    {
        SetExactMeterCharge(0);
        StartCoroutine(ImmunityRoutine());
        Debug.Log("PARRIED");
    }

    private IEnumerator ImmunityRoutine()
    {
        damageImmune = true;
        Debug.Log("Player DMG Immune");
        yield return new WaitForSeconds(immunityTime);
        damageImmune = false;
        Debug.Log("Player can be damaged");
    }

    private void CheckHp()
    {
        if (currentHp <= 0)
        {
            Debug.Log("GAME OVER.");
        }
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        Debug.Log("Player Stunned");
        ActionMapsManager.Instance.SwapActionMap("RestrictedInput");

        yield return new WaitForSeconds(stunTime);

        isStunned = false;
        ActionMapsManager.Instance.SwapActionMap("Gameplay");
        Debug.Log("Player no longer Stunned");
    }
}