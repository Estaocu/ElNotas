using UnityEngine;

public class MelodyListener : MonoBehaviour
{
    public Melody desiredMelody;

    private void OnTriggerEnter(Collider other)
    {
        var soundwave = other.GetComponent<Soundwave>();
        if (!soundwave) return;

        var reactor = GetComponent<IReactToMelody>();
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
