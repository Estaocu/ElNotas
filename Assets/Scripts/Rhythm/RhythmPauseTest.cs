using UnityEngine;

public class RhythmPauseTest : MonoBehaviour
{
    [SerializeField] private RhythmClock rhythmClock;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (rhythmClock.IsPaused)
                rhythmClock.Resume();
            else
                rhythmClock.Pause();
        }
    }
}