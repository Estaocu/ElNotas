using UnityEngine;

public class LifeAndMeter : MonoBehaviour
{
    [Header("settings")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private int maxMeter = 10;
    [SerializeField] private int immunityTime = 16;

    // Campos privados (Estado interno)
    private int currentHp;
    private int currentMeter;
    private bool damageImmune;

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
            currentHp = Mathf.Max(currentHp - dmg, 0);
            SetExactMeterCharge(0);
            CheckHp();
            Debug.Log("Ai quin mal...");
        }
    }

    public void ChangeMeterCharge(int q)
    {
        currentMeter = Mathf.Clamp(currentMeter + q, 0, maxMeter);
        Debug.Log($"meter charge: {currentMeter} / {maxMeter}");
    }

    public void SetExactMeterCharge(int q)
    {
        currentMeter = Mathf.Clamp(q, 0, maxMeter);
    }

    private void DoParry()
    {
        SetExactMeterCharge(0);
        TriggerImmunity();
        Debug.Log("PARRIED");
    }

    private void TriggerImmunity()
    {
        damageImmune = true;
        RhythmBeatWaiter.WaitForSubBeats(immunityTime, BeatWaitMode.Immediate, () => 
            damageImmune = false);
    }

    private void CheckHp()
    {
        if (currentHp <= 0)
        {
            Debug.Log("GAME OVER.");
        }
    }

    public void GetStunned()
    {
        Debug.Log("stunneao");
    }
}