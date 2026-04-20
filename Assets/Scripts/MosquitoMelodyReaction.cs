using UnityEngine;

public class MosquitoMelodyReaction : MonoBehaviour, IReactToMelody
{
    private HostileEntityAI hostile;

    private void Awake() { hostile = GetComponent<HostileEntityAI>(); }

    public void React(Melody receivedMelody)
    {
        Debug.Log($"[MosquitoMelodyReaction] Received melody: {receivedMelody?.melodyName}, calling CalmDown()");
        hostile.CalmDown();
    }
}
