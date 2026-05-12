using UnityEngine;

public class FlowerStepped : MonoBehaviour
{
    public bool canBufferMelody = true;
    [SerializeField] private Singer singer;
    [SerializeField] private AddNotesOnBeat beatSinger;
    [SerializeField] private Melody melody;

    private void Awake()
    {
        if (singer == null) singer = GetComponent<Singer>();
        if (beatSinger == null) beatSinger = GetComponent<AddNotesOnBeat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canBufferMelody) return;

        ExecuteFlowerLogic();
    }

    private void ExecuteFlowerLogic()
    {
        if (beatSinger != null) beatSinger.Sing();
    }
}