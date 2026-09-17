using CMF;
using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerHP : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private int stunTime = 4;
    [SerializeField] private int immunityTime = 8;

    // Campos privados (Estado interno)
    private int currentHp;
    private float currentMeter => meter.CurrentMeter;
    private bool damageImmune;
    private bool isStunned;

    // Propiedades calculadas (Solo lectura para el exterior)
    public bool isMeterFull => meter.HasFullMeter;
    public int GetCurrentHp() => currentHp;

    [SerializeField] private PlayerRhythmController meter;

    private RhythmClock rhythmClock;

    private void Awake()
    {
        currentHp = maxHp;
        rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    public void OnHit(int dmg)
    {
        if (damageImmune) return;

        if (isMeterFull)
        {
            DoParry();
        }
        else
        {
            if(currentMeter != 0)
            {
                meter.ConsumeFullMeter();
                StartCoroutine(StunRoutine());
                return;
            }

            currentHp = Mathf.Max(currentHp - dmg, 0);
            meter.ConsumeFullMeter();
            Debug.Log($"HITS LEFT: {currentHp} / {maxHp}");
            CheckHp();
            
        }
    }
    private void DoParry()
    {
        meter.ConsumeFullMeter();
        StartCoroutine(ImmunityRoutine());
        Debug.Log("PARRIED");
    }

    private IEnumerator ImmunityRoutine()
    {
        damageImmune = true;
        Debug.Log("Player DMG Immune");
        yield return rhythmClock.WaitForSubBeats(immunityTime);
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

        yield return rhythmClock.WaitForSubBeats(stunTime);

        isStunned = false;
        ActionMapsManager.Instance.SwapActionMap("Gameplay");
        Debug.Log("Player no longer Stunned");
    }
}