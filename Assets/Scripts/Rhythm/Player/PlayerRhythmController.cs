using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerRhythmController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmClock rhythmClock;
    [SerializeField] private RhythmQuantizer rhythmQuantizer;
    [SerializeField] private TextMeshProUGUI meterText;

    [Header("Meter")]
    [SerializeField] private float maxMeter = 100.0f;
    [SerializeField] private float secondSubBeatPoints = 6.0f;

    [Header("Continuity Bonus")]
    [Tooltip("Maximum multiplier reached by maintaining a valid rhythm.")]
    [SerializeField] private float maxContinuityMultiplier = 1.25f;

    [Tooltip("Controls how the continuity bonus grows.")]
    [SerializeField] private AnimationCurve continuityCurve =
        AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

    [Header("Cadence Bonus")]
    [Tooltip("Maximum additional multiplier for consecutive subbeats.")]
    [SerializeField] private float maxCadenceMultiplier = 1.0f;

    [Tooltip("Minimum cadence multiplier applied when notes have gaps.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float minimumCadenceMultiplier = 0.35f;

    [Header("Input Protection")]
    [Tooltip("Minimum physical time between two accepted note inputs.")]
    [SerializeField] private double minimumNoteInterval = 0.05;

    [Header("Spam Detection")]
    [SerializeField] private bool spamDetectionEnabled = true;

    [Tooltip("Number of note inputs required to trigger rapid-input spam.")]
    [SerializeField] private int rapidInputCount = 3;

    [Tooltip("Maximum time window for rapid-input spam detection.")]
    [SerializeField] private double rapidInputWindowSeconds = 0.65;

    [Header("Overheat")]
    [SerializeField] private bool logSpam = true;

    private readonly List<PlayedNote> currentNotes =
        new List<PlayedNote>();

    private readonly Queue<double> recentInputTimes =
        new Queue<double>();

    private PlayerInputs input;

    private float currentMeter;
    private int continuityCount;

    private RhythmPosition lastAcceptedPosition;
    private double lastAcceptedTargetDspTime;
    private double lastAcceptedInputDspTime;

    private bool hasLastAcceptedNote;

    private bool isOverheated;
    private int overheatEndBar;

    public IReadOnlyList<PlayedNote> CurrentNotes => currentNotes;

    public float CurrentMeter => currentMeter;

    public float MaxMeter => maxMeter;

    public float FirstSubBeatPoints =>
        secondSubBeatPoints / 1.5f;

    public bool IsOverheated => isOverheated;

    public bool HasFullMeter =>
        currentMeter >= maxMeter;

    public event Action<PlayedNote> OnNoteAccepted;
    public event Action OnNoteRejected;
    public event Action OnNoteTooClose;
    public event Action OnMelodyCleared;
    public event Action OnSpam;
    public event Action OnOverheatStarted;
    public event Action OnOverheatEnded;
    public event Action<float> OnMeterChanged;

    private void Awake()
    {
        input = new PlayerInputs();
    }

    private void OnEnable()
    {
        input.Gameplay.Note1.performed += OnNote1Played;
        input.Gameplay.Note2.performed += OnNote2Played;
        input.Gameplay.Note3.performed += OnNote3Played;
        input.Gameplay.Note4.performed += OnNote4Played;

        input.Gameplay.Enable();

        RhythmClock.OnBar += HandleBar;
    }

    private void OnDisable()
    {
        input.Gameplay.Note1.performed -= OnNote1Played;
        input.Gameplay.Note2.performed -= OnNote2Played;
        input.Gameplay.Note3.performed -= OnNote3Played;
        input.Gameplay.Note4.performed -= OnNote4Played;

        input.Gameplay.Disable();

        RhythmClock.OnBar -= HandleBar;
    }

    private void OnValidate()
    {
        maxMeter = Mathf.Max(0.0f, maxMeter);
        secondSubBeatPoints = Mathf.Max(0.0f, secondSubBeatPoints);

        maxContinuityMultiplier =
            Mathf.Max(1.0f, maxContinuityMultiplier);

        maxCadenceMultiplier =
            Mathf.Max(0.0f, maxCadenceMultiplier);

        minimumNoteInterval =
            Math.Max(0.0, minimumNoteInterval);

        rapidInputCount =
            Mathf.Max(2, rapidInputCount);

        rapidInputWindowSeconds =
            Math.Max(0.01, rapidInputWindowSeconds);
    }

    private void OnNote1Played(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
            ProcessNote(notesEnum.Note1);
    }

    private void OnNote2Played(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
            ProcessNote(notesEnum.Note2);
    }

    private void OnNote3Played(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
            ProcessNote(notesEnum.Note3);
    }

    private void OnNote4Played(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
            ProcessNote(notesEnum.Note4);
    }

    public bool ProcessNote(notesEnum note)
    {
        if (rhythmClock == null || rhythmQuantizer == null)
        {
            Debug.LogError(
                "PlayerRhythmController requires a RhythmClock and RhythmQuantizer."
            );

            return false;
        }

        double inputDspTime = AudioSettings.dspTime;

        RhythmQuantizationResult result =
            rhythmQuantizer.Quantize(inputDspTime);

        if (IsOverheatActive(result.position))
            return false;

        if (spamDetectionEnabled &&
            IsSpam(inputDspTime, result))
        {
            TriggerSpam(result.position);
            return false;
        }

        if (!result.isValid)
        {
            HandleRhythmMiss();
            return false;
        }

        if (IsInputTooClose(inputDspTime))
        {
            OnNoteTooClose?.Invoke();
            return false;
        }

        if (IsSlotAlreadyOccupied(result.position))
        {
            OnNoteTooClose?.Invoke();
            return false;
        }

        AcceptNote(note, result);

        return true;
    }

    private bool IsSpam(
        double inputDspTime,
        RhythmQuantizationResult result)
    {
        return RegisterRapidInput(inputDspTime) ||
               IsSameSlotSpam(result);
    }

    private bool RegisterRapidInput(double inputDspTime)
    {
        recentInputTimes.Enqueue(inputDspTime);

        while (
            recentInputTimes.Count > 0 &&
            inputDspTime - recentInputTimes.Peek() >=
            rapidInputWindowSeconds)
        {
            recentInputTimes.Dequeue();
        }

        return recentInputTimes.Count >= rapidInputCount;
    }

    private bool IsSameSlotSpam(
        RhythmQuantizationResult result)
    {
        if (!hasLastAcceptedNote)
            return false;

        return Math.Abs(
            result.targetDspTime -
            lastAcceptedTargetDspTime
        ) < 0.000001;
    }

    private bool IsInputTooClose(double inputDspTime)
    {
        if (!hasLastAcceptedNote)
            return false;

        return
            inputDspTime -
            lastAcceptedInputDspTime <
            minimumNoteInterval;
    }

    private bool IsSlotAlreadyOccupied(
        RhythmPosition position)
    {
        int targetSubBeat =
            GetAbsoluteSubBeat(position);

        for (int i = 0; i < currentNotes.Count; i++)
        {
            if (
                GetAbsoluteSubBeat(
                    currentNotes[i].position
                ) == targetSubBeat
            )
            {
                return true;
            }
        }

        return false;
    }

    private void AcceptNote(
        notesEnum note,
        RhythmQuantizationResult result)
    {
        ClearMelodyIfNewBar(result.position);

        bool hasContinuity =
            HasRhythmContinuity(result.position);

        if (hasContinuity)
            continuityCount++;
        else
            continuityCount = 0;

        PlayedNote playedNote =
            new PlayedNote(
                note,
                result.position,
                result.inputDspTime
            );

        currentNotes.Add(playedNote);

        float points =
            CalculateMeterGain(
                result.position,
                continuityCount
            );

        SetMeter(currentMeter + points);

        if (meterText != null)
            meterText.SetText(currentMeter.ToString());

        lastAcceptedPosition =
            result.position;

        lastAcceptedTargetDspTime =
            result.targetDspTime;

        lastAcceptedInputDspTime =
            result.inputDspTime;

        hasLastAcceptedNote = true;

        OnNoteAccepted?.Invoke(playedNote);
    }

    private bool HasRhythmContinuity(
        RhythmPosition position)
    {
        if (!hasLastAcceptedNote)
            return false;

        return true;
    }

    private float CalculateMeterGain(
        RhythmPosition position,
        int currentContinuityCount)
    {
        float basePoints =
            position.subBeat %
            RhythmClock.SubBeatsPerBeat == 0
                ? FirstSubBeatPoints
                : secondSubBeatPoints;

        float continuityMultiplier =
            GetContinuityMultiplier(
                currentContinuityCount
            );

        float cadenceMultiplier =
            GetCadenceMultiplier(position);

        return
            basePoints *
            continuityMultiplier *
            cadenceMultiplier *
            GetMeterNormalization();
    }

    private float GetContinuityMultiplier(
        int continuityIndex)
    {
        if (continuityIndex <= 0)
            return 1.0f;

        const int maximumStreak = 16;

        float normalized =
            Mathf.Clamp01(
                continuityIndex /
                (float)(maximumStreak - 1)
            );

        float curveValue =
            continuityCurve.Evaluate(normalized);

        return Mathf.Lerp(
            1.0f,
            maxContinuityMultiplier,
            curveValue
        );
    }

    private float GetCadenceMultiplier(
        RhythmPosition position)
    {
        if (!hasLastAcceptedNote)
            return 1.0f;

        int previousSlot =
            GetAbsoluteSubBeat(lastAcceptedPosition);

        int currentSlot =
            GetAbsoluteSubBeat(position);

        int distance =
            currentSlot - previousSlot;

        if (distance <= 0)
            return 1.0f;

        if (distance == 1)
        {
            return 1.0f + maxCadenceMultiplier;
        }

        float normalizedGap =
            Mathf.Clamp01(
                (distance - 1) / 3.0f
            );

        float cadenceFactor =
            Mathf.Lerp(
                1.0f,
                minimumCadenceMultiplier,
                normalizedGap
            );

        return
            1.0f +
            maxCadenceMultiplier *
            cadenceFactor;
    }

    private float GetMeterNormalization()
    {
        const int maximumStreak = 16;

        float totalPoints = 0.0f;

        RhythmPosition previousPosition =
            new RhythmPosition(0, 0, 0);

        bool previousExists = false;

        for (int i = 0; i < maximumStreak; i++)
        {
            int subBeat =
                i % RhythmClock.SubBeatsPerBar;

            float basePoints =
                subBeat %
                RhythmClock.SubBeatsPerBeat == 0
                    ? FirstSubBeatPoints
                    : secondSubBeatPoints;

            float continuityMultiplier =
                GetContinuityMultiplier(i);

            float cadenceMultiplier = 1.0f;

            if (previousExists)
            {
                int previousSlot =
                    GetAbsoluteSubBeat(
                        previousPosition
                    );

                int distance =
                    i - previousSlot;

                if (distance == 1)
                {
                    cadenceMultiplier =
                        1.0f +
                        maxCadenceMultiplier;
                }
            }

            totalPoints +=
                basePoints *
                continuityMultiplier *
                cadenceMultiplier;

            previousPosition =
                new RhythmPosition(
                    i / RhythmClock.SubBeatsPerBar,
                    subBeat /
                    RhythmClock.SubBeatsPerBeat,
                    subBeat
                );

            previousExists = true;
        }

        if (totalPoints <= 0.0f)
            return 0.0f;

        return maxMeter / totalPoints;
    }

    private void HandleRhythmMiss()
    {
        ResetContinuity();
        OnNoteRejected?.Invoke();
    }

    private void TriggerSpam(
        RhythmPosition position)
    {
        if (logSpam)
        {
            Debug.LogWarning(
                $"RHYTHM SPAM DETECTED | " +
                $"Bar: {position.bar} | " +
                $"SubBeat: {position.subBeat}"
            );
        }

        recentInputTimes.Clear();

        ClearMelody();
        ResetContinuity();
        SetMeter(0.0f);

        if (meterText != null)
            meterText.SetText(
                currentMeter.ToString()
            );

        StartOverheat(position.bar);

        OnSpam?.Invoke();
    }

    private void StartOverheat(int currentBar)
    {
        isOverheated = true;
        overheatEndBar = currentBar + 2;

        OnOverheatStarted?.Invoke();

        if (logSpam)
        {
            Debug.LogWarning(
                $"INSTRUMENT OVERHEATED | " +
                $"Available at bar {overheatEndBar}"
            );
        }
    }

    private bool IsOverheatActive(
        RhythmPosition position)
    {
        if (!isOverheated)
            return false;

        if (position.bar < overheatEndBar)
            return true;

        EndOverheat();

        return false;
    }

    private void EndOverheat()
    {
        isOverheated = false;

        OnOverheatEnded?.Invoke();

        if (logSpam)
            Debug.Log("INSTRUMENT OVERHEAT ENDED");
    }

    private void HandleBar(RhythmTick tick)
    {
        if (isOverheated &&
            tick.position.bar >= overheatEndBar)
        {
            EndOverheat();
        }

        if (currentNotes.Count == 0)
            return;

        int lastNoteBar =
            currentNotes[
                currentNotes.Count - 1
            ].position.bar;

        if (tick.position.bar > lastNoteBar)
            ClearMelody();
    }

    private void ClearMelodyIfNewBar(
        RhythmPosition position)
    {
        if (currentNotes.Count == 0)
            return;

        int lastNoteBar =
            currentNotes[
                currentNotes.Count - 1
            ].position.bar;

        if (position.bar > lastNoteBar)
            ClearMelody();
    }

    public void ClearMelody()
    {
        if (currentNotes.Count == 0)
            return;

        currentNotes.Clear();

        OnMelodyCleared?.Invoke();
    }

    private void ResetContinuity()
    {
        continuityCount = 0;
        hasLastAcceptedNote = false;
        lastAcceptedTargetDspTime = 0.0;
        lastAcceptedInputDspTime = 0.0;
    }

    private int GetAbsoluteSubBeat(
        RhythmPosition position)
    {
        return
            position.bar *
            RhythmClock.SubBeatsPerBar +
            position.subBeat;
    }

    public void SetMeter(float value)
    {
        currentMeter =
            Mathf.Clamp(
                value,
                0.0f,
                maxMeter
            );

        OnMeterChanged?.Invoke(currentMeter);
    }

    public bool ConsumeFullMeter()
    {
        if (!HasFullMeter)
            return false;

        SetMeter(0.0f);

        if (meterText != null)
            meterText.SetText(
                currentMeter.ToString()
            );

        return true;
    }

    [Serializable]
    public struct PlayedNote
    {
        public notesEnum note;
        public RhythmPosition position;
        public double inputDspTime;

        public PlayedNote(
            notesEnum note,
            RhythmPosition position,
            double inputDspTime)
        {
            this.note = note;
            this.position = position;
            this.inputDspTime = inputDspTime;
        }
    }
}