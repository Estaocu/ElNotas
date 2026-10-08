using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetronomeManager : MonoBehaviour
{
    private List<MetronomeBar> bars = new List<MetronomeBar>();
    private RhythmClock clock;
    [SerializeField] private float delay;

    [SerializeField] private PlayerRhythmController instrument;

    private bool alreadyUp = false;

    private void OnEnable()
    {
        RhythmClock.OnSubBeat += OnSubBeat;
        if (instrument != null)
        {
            instrument.OnMelodyCleared += BarsDown;
            instrument.OnNoteAccepted += BarsUp;
        }
    }

    void Start()
    {
        bars = new List<MetronomeBar>(GetComponentsInChildren<MetronomeBar>(true));
        clock = FindFirstObjectByType<RhythmClock>();
    }


    private void OnDisable()
    {
        RhythmClock.OnSubBeat -= OnSubBeat;
    }

    private void OnSubBeat(RhythmTick tick)
    {
        int subBeat = tick.position.subBeat;
        if(subBeat%2 == 0) return; //solo impares

        int id = subBeat/2 +1;
        if (id >= 4) id = 0;
        //Debug.Log(id);

        StartCoroutine(HoldOrderRoutine(id));
        

    }
    public static float ToSingle(double value)
    {
        return (float)value;
    }

    private IEnumerator HoldOrderRoutine(int id)
    {
        float time = ToSingle(clock.SubBeatDuration);

        yield return new WaitForSeconds(time*delay);

        bars[id].Bounce(id);

    }

    private void BarsUp(PlayerRhythmController.PlayedNote playedNote)
    {
        if(alreadyUp) return;

        foreach (MetronomeBar bar in bars)
        {
            bar.Deploy(true);
        }
        alreadyUp = true;
    }

    private void BarsDown()
    {
        foreach (MetronomeBar bar in bars)
        {
            bar.Deploy(false);
        }
        alreadyUp = false;
    }

    public void ToggleVisibility(bool desiredState)
    {
        foreach (MetronomeBar bar in bars)
        {
            bar.ToggleVisibility(desiredState);
        }
    }
}
