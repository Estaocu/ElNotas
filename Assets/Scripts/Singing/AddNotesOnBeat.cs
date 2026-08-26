using System.Collections;
using UnityEngine;

public class AddNotesOnBeat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Singer singer;
    [SerializeField] private RhythmClock rhythmClock;

    [Header("NPC Rhythm Pattern")]
    [SerializeField] private PatternMode patternMode = PatternMode.Local;

    // One slot represents one eighth note.
    public NoteSlot[] pattern =
        new NoteSlot[RhythmClock.SubBeatsPerBar];

    [SerializeField, Range(0f, 0.25f)]
    private float humanizationPercent = 0.05f;

    public bool IsSinging =>
        isPatternActive || waitingForStart;

    private bool isPatternActive;
    private bool waitingForStart;

    private int nextSlotIndex;

    private bool hasExplicitStart;
    private long explicitStartAbsoluteSubBeat;

    private void Awake()
    {
        if (singer == null)
            singer = GetComponent<Singer>();

        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    private void OnValidate()
    {
        int requiredLength =
            RhythmClock.SubBeatsPerBar;

        if (pattern == null ||
            pattern.Length != requiredLength)
        {
            NoteSlot[] resized =
                new NoteSlot[requiredLength];

            if (pattern != null)
            {
                int copy =
                    Mathf.Min(
                        pattern.Length,
                        requiredLength
                    );

                for (int i = 0; i < copy; i++)
                {
                    resized[i] = pattern[i];
                }
            }

            pattern = resized;
        }

        humanizationPercent =
            Mathf.Clamp(
                humanizationPercent,
                0f,
                0.25f
            );
    }

    private void OnEnable()
    {
        RhythmClock.OnSubBeat += OnSubBeat;
    }

    private void OnDisable()
    {
        RhythmClock.OnSubBeat -= OnSubBeat;
    }

    public void Sing()
    {
        if (pattern == null)
            return;

        hasExplicitStart = false;

        waitingForStart = true;
        isPatternActive = false;
    }

    public void SingFromAbsoluteSubBeat(
        long absoluteSubBeat)
    {
        if (pattern == null ||
            pattern.Length < RhythmClock.SubBeatsPerBar)
        {
            return;
        }

        hasExplicitStart = true;
        explicitStartAbsoluteSubBeat =
            absoluteSubBeat;

        waitingForStart = true;
        isPatternActive = false;
        nextSlotIndex = 0;
    }

    public void ScheduleNoteAtAbsoluteSubBeat(
        notesEnum note,
        long absoluteSubBeat)
    {
        if (singer == null)
            return;

        if (rhythmClock == null)
        {
            rhythmClock =
                FindFirstObjectByType<RhythmClock>();
        }

        if (rhythmClock == null ||
            !rhythmClock.IsRunning ||
            rhythmClock.IsPaused)
        {
            return;
        }

        double targetDspTime =
            rhythmClock.StartDspTime +
            absoluteSubBeat *
            rhythmClock.SubBeatDuration;

        ScheduleNote(
            note,
            targetDspTime
        );
    }

    public void ScheduleTwoNotesAtAbsoluteSubBeats(
        notesEnum firstNote,
        long firstAbsoluteSubBeat,
        notesEnum secondNote,
        long secondAbsoluteSubBeat)
    {
        ScheduleNoteAtAbsoluteSubBeat(
            firstNote,
            firstAbsoluteSubBeat
        );

        ScheduleNoteAtAbsoluteSubBeat(
            secondNote,
            secondAbsoluteSubBeat
        );
    }

    public void StopSinging()
    {
        waitingForStart = false;
        isPatternActive = false;

        hasExplicitStart = false;
        nextSlotIndex = 0;
    }

    private void OnSubBeat(RhythmTick tick)
    {
        if (pattern == null ||
            pattern.Length < RhythmClock.SubBeatsPerBar)
        {
            return;
        }

        long currentAbsoluteSubBeat =
            GetAbsoluteSubBeat(tick.position);

        if (waitingForStart)
        {
            if (hasExplicitStart)
            {
                if (currentAbsoluteSubBeat <
                    explicitStartAbsoluteSubBeat)
                {
                    return;
                }

                if (currentAbsoluteSubBeat >
                    explicitStartAbsoluteSubBeat)
                {
                    waitingForStart = false;
                    isPatternActive = false;
                    hasExplicitStart = false;
                    nextSlotIndex = 0;

                    return;
                }

                waitingForStart = false;
                isPatternActive = true;
                nextSlotIndex = 0;
            }
            else
            {
                if (!IsValidStartPosition(tick.position))
                    return;

                waitingForStart = false;
                isPatternActive = true;
                nextSlotIndex = 0;
            }
        }

        if (!isPatternActive)
            return;

        NoteSlot slot =
            pattern[nextSlotIndex];

        if (slot != NoteSlot.Empty)
        {
            ScheduleNote(
                (notesEnum)slot,
                tick.dspTime
            );
        }

        nextSlotIndex++;

        if (nextSlotIndex >=
            RhythmClock.SubBeatsPerBar)
        {
            isPatternActive = false;
            nextSlotIndex = 0;
            hasExplicitStart = false;
        }
    }

    private bool IsValidStartPosition(
        RhythmPosition position)
    {
        if (patternMode == PatternMode.Absolute)
        {
            return position.subBeat == 0;
        }

        return
            position.subBeat %
            RhythmClock.SubBeatsPerBeat == 0;
    }

    private void ScheduleNote(
        notesEnum note,
        double baseDspTime)
    {
        if (rhythmClock == null)
            return;

        double humanization =
            (double)Random.Range(
                -humanizationPercent,
                humanizationPercent
            ) *
            rhythmClock.SubBeatDuration;

        double targetDspTime =
            baseDspTime + humanization;

        StartCoroutine(
            PerformNoteAt(
                note,
                targetDspTime
            )
        );
    }

    private IEnumerator PerformNoteAt(
        notesEnum note,
        double targetDspTime)
    {
        while (
            AudioSettings.dspTime <
            targetDspTime)
        {
            yield return null;
        }

        if (singer != null)
        {
            singer.AddNote(note);
        }
    }

    private static long GetAbsoluteSubBeat(
        RhythmPosition position)
    {
        return
            (long)position.bar *
            RhythmClock.SubBeatsPerBar +
            position.subBeat;
    }
}