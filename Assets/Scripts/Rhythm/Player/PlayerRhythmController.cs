using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRhythmController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmClock rhythmClock;
    [SerializeField] private RhythmQuantizer rhythmQuantizer;
    [SerializeField] private Meter meter;

    [Header("Input Protection")]
    [Tooltip("Minimum physical time between two accepted note inputs.")]
    [SerializeField] private double minimumNoteInterval = 0.05;

    [Tooltip("Number of note inputs required to trigger rapid-input spam.")]
    [SerializeField] private int rapidInputCount = 3;

    [Tooltip("Maximum time window for rapid-input spam detection.")]
    [SerializeField] private double rapidInputWindowSeconds = 0.65;

    private readonly List<PlayedNote> currentNotes = new List<PlayedNote>();

    private readonly Queue<double> recentInputTimes = new Queue<double>();

    private PlayerInputs input;

    private RhythmPosition lastAcceptedPosition;
    private double lastAcceptedTargetDspTime;
    private double lastAcceptedInputDspTime;

    private bool hasLastAcceptedNote;

    public IReadOnlyList<PlayedNote> CurrentNotes => currentNotes;

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
        meter = GetComponent<Meter>();
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
        minimumNoteInterval =
        Math.Max(0.0, minimumNoteInterval);

        rapidInputCount =
        Mathf.Max(2, rapidInputCount);

        rapidInputWindowSeconds =
        Math.Max(0.01, rapidInputWindowSeconds);
    }

    private void OnNote1Played(InputAction.CallbackContext context)
    {
        if (context.performed) 
        ProcessNote(notesEnum.Note1);
    }

    private void OnNote2Played(InputAction.CallbackContext context)
    {
        if (context.performed)
        ProcessNote(notesEnum.Note2);
    }

    private void OnNote3Played(InputAction.CallbackContext context)
    {
        if (context.performed)
        ProcessNote(notesEnum.Note3);
    }

    private void OnNote4Played(InputAction.CallbackContext context)
    {
        if (context.performed)
        ProcessNote(notesEnum.Note4);
    }

    public bool ProcessNote(notesEnum note)
    {
        if (rhythmClock == null || rhythmQuantizer == null)
        {
            Debug.LogError("PlayerRhythmController requires a RhythmClock and RhythmQuantizer.");

            return false;
        }

        double inputDspTime = AudioSettings.dspTime;

        RhythmQuantizationResult result = rhythmQuantizer.Quantize(inputDspTime);

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
        meter.IncreaseByOne();

        return true;
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
        if (!hasLastAcceptedNote) return false;

        return Math.Abs(result.targetDspTime -lastAcceptedTargetDspTime) < 0.000001;
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
            if (GetAbsoluteSubBeat(
                currentNotes[i].position
                ) == targetSubBeat)
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
        PlayedNote playedNote =
            new PlayedNote(
                note,
                result.position,
                result.inputDspTime
            );

        currentNotes.Add(playedNote);

        lastAcceptedPosition = result.position;

        lastAcceptedTargetDspTime = result.targetDspTime;

        lastAcceptedInputDspTime = result.inputDspTime;

        hasLastAcceptedNote = true;

        OnNoteAccepted?.Invoke(playedNote);
    }

    private void HandleRhythmMiss()
    {
        OnNoteRejected?.Invoke();
    }

    private void HandleBar(RhythmTick tick)
    {
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

    private int GetAbsoluteSubBeat(RhythmPosition position)
    {
        return position.bar * RhythmClock.SubBeatsPerBar + position.subBeat;
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