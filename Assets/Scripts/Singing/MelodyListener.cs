using UnityEngine;

public class MelodyListener : MonoBehaviour
{
    public Melody desiredMelody;

    private IReactToMelody reactor;
    private Singer selfSinger;

    private void Awake()
    {
        reactor = GetComponent<IReactToMelody>();
        selfSinger = GetComponent<Singer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var soundwave = other.GetComponentInParent<Soundwave>();
        if (!soundwave) return;
        if (selfSinger != null && soundwave.source == selfSinger) return;
        if (reactor == null) return;

        if (desiredMelody != null)
        {
            if (desiredMelody == soundwave.myMelody)
                reactor.React(soundwave.myMelody);
            return;
        }

        reactor.React(soundwave.myMelody);
    }
}
