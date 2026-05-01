using UnityEngine;

public class FlowerStepped : MonoBehaviour
{
    public bool canBufferMelody = true;
    [SerializeField] private Singer singer;
    [SerializeField] private Melody melody;

    private void OnTriggerEnter(Collider other)
    {
        if (!canBufferMelody) return;

        ExecuteFlowerLogic();
    }

    private void ExecuteFlowerLogic()
    {
        singer.Sing();
    }
}