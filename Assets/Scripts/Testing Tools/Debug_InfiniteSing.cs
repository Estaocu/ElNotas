using UnityEngine;

public class Debug_InfiniteSing : MonoBehaviour
{
    [SerializeField] private AddNotesOnBeat beatSinger;
    public bool autoRepeat = true;

    private void Awake()
    {
        if (beatSinger == null) beatSinger = GetComponent<AddNotesOnBeat>();
    }

    private void Start()
    {
        if (beatSinger != null) beatSinger.Sing();
    }

    void Update()
    {
        if (!autoRepeat || beatSinger == null) return;

        // Si ha terminado su patrón de 16 notas, le pedimos que empiece otra vez
        if (!beatSinger.IsSinging)
        {
            beatSinger.Sing();
        }
    }
}
