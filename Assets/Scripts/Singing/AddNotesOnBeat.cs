using System.Collections;
using UnityEngine;

public class AddNotesOnBeat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Singer singer;

    [Header("NPC Rhythm Pattern")]
    [SerializeField] private PatternMode patternMode = PatternMode.Local;
    public NoteSlot[] pattern = new NoteSlot[16];
    [SerializeField, Range(0f, 0.25f)] private float humanizationPercent = 0.05f;

    public bool IsSinging => isPatternActive || waitingForStart;

    private bool isPatternActive;
    private bool waitingForStart;
    private int nextSlotIndex;
    private int slotsRemaining;

    private void Awake()
    {
        if (singer == null) singer = GetComponent<Singer>();
    }

    private void OnValidate()
    {
        if (pattern == null || pattern.Length != 16)
        {
            var resized = new NoteSlot[16];
            if (pattern != null)
            {
                int copy = Mathf.Min(pattern.Length, 16);
                for (int i = 0; i < copy; i++) resized[i] = pattern[i];
            }
            pattern = resized;
        }
    }

    private void OnEnable()
    {
        RhythmManager.OnBeatChanged += OnBeatChanged;
    }

    private void OnDisable()
    {
        RhythmManager.OnBeatChanged -= OnBeatChanged;
    }

    public void Sing()
    {
        waitingForStart = true;
    }

    public void StopSinging()
    {
        waitingForStart = false;
        isPatternActive = false;
    }

    private void OnBeatChanged(int beatOf16)
    {
        if (pattern == null || pattern.Length < 16) return;

        int nextSubBeat = beatOf16 >= 16 ? 1 : beatOf16 + 1;

        // 1. Alinear arranque.
        if (waitingForStart)
        {
            bool aligned =
                (patternMode == PatternMode.Local && IsWholeBeat(nextSubBeat)) ||
                (patternMode == PatternMode.Absolute && nextSubBeat == 1);

            if (!aligned) return;

            waitingForStart = false;
            isPatternActive = true;
            nextSlotIndex = 0;
            slotsRemaining = 16;
        }

        if (!isPatternActive) return;

        // 2. Programar el slot que debe sonar.
        NoteSlot slot = pattern[nextSlotIndex];
        if (slot != NoteSlot.Empty)
        {
            var rm = RhythmManager.Instance;
            if (rm != null)
            {
                double target = rm.NextSubBeatDspTime + rm.SubBeatDuration
                                + Random.Range(-humanizationPercent, humanizationPercent)
                                  * rm.SubBeatDuration;

                StartCoroutine(PerformNoteAt((notesEnum)slot, target));
            }
        }

        // 3. Avanzar el cursor.
        nextSlotIndex++;
        slotsRemaining--;
        if (slotsRemaining <= 0)
            isPatternActive = false;
    }

    private static bool IsWholeBeat(int subBeat)
    {
        return ((subBeat - 1) % 4) == 0;
    }

    private IEnumerator PerformNoteAt(notesEnum note, double targetDspTime)
    {
        while (AudioSettings.dspTime < targetDspTime)
            yield return null;

        if (singer != null)
            singer.AddNote(note);
    }
}
