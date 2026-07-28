using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SingleNotesListener))]
public class Elevator : MonoBehaviour
{
    [System.Serializable]
    public class FloorData
    {
        public float height = 0f;
        public notesEnum[] requiredMelody = new notesEnum[4];
    }

    [Header("Floor Settings")]
    [SerializeField] private List<FloorData> floors = new List<FloorData>();
    [SerializeField] private int subBeatsPerFloor = 4;

    [Header("Current State")]
    [SerializeField] private int currentFloorIndex = 0;

    private Rigidbody rb;
    private SingleNotesListener notesListener;
    private Vector3 initialWorldPosition;

    private bool isMoving = false;
    private bool isPlayerOnBoard = false;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveProgress = 0f;
    private float moveDuration = 1f;
    private BeatWaitHandle currentWaitHandle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        initialWorldPosition = transform.position;

        notesListener = GetComponent<SingleNotesListener>();
        notesListener.currentMode = SingleNotesListener.ListenerMode.PredefinedMelodies;
        notesListener.maxBufferLength = 4;
        notesListener.clearOnFail = true;
        notesListener.wantsToListen = false;

        notesListener.OnNoteReceivedEvent.AddListener(HandleNoteReceived);
        notesListener.OnMelodyMatchedEvent.AddListener(HandleMelodyMatched);
        notesListener.OnMelodyFailedEvent.AddListener(HandleMelodyFailed);
    }

    private void Start()
    {
        if (floors != null && floors.Count > currentFloorIndex)
        {
            Vector3 targetPos = GetWorldPositionForFloor(currentFloorIndex);
            rb.position = targetPos;
            transform.position = targetPos;
        }

        UpdateListenerMelodies();
    }

    private void UpdateListenerMelodies()
    {
        List<notesEnum[]> elevatorMelodies = new List<notesEnum[]>();
        foreach (FloorData floor in floors)
        {
            elevatorMelodies.Add(floor.requiredMelody);
        }
        notesListener.SetDesiredMelodies(elevatorMelodies);
    }

    // ==========================================
    // PUBLIC TRIGGER CALLBACKS
    // ==========================================

    public void OnPlayerEntered()
    {
        isPlayerOnBoard = true;
        Debug.Log("<color=green>[ELEVATOR] Player entered platform. Note listening ENABLED.</color>");

        if (!isMoving)
        {
            notesListener.wantsToListen = true;
        }
    }

    public void OnPlayerExited()
    {
        isPlayerOnBoard = false;
        notesListener.wantsToListen = false;
        notesListener.ResetBuffer();
        Debug.Log("<color=orange>[ELEVATOR] Player exited platform. Note listening DISABLED and buffer cleared.</color>");
    }

    public void OnNoteDetected(GameObject noteObject)
    {
        if (notesListener != null)
        {
            notesListener.ProcessIncomingObject(noteObject);
        }
    }

    // ==========================================
    // NOTE LISTENER CALLBACKS
    // ==========================================

    private void HandleNoteReceived(notesEnum note, Transform author)
    {
        Debug.Log($"[ELEVATOR] Note received: <color=yellow>{note}</color> (From: {author.name})");
        PlayNoteFeedback(note);
    }

    private void HandleMelodyMatched(notesEnum[] matchedMelody, Transform author)
    {
        int matchedFloorIndex = -1;

        for (int i = 0; i < floors.Count; i++)
        {
            if (SingleNotesListener.CompareArrays(floors[i].requiredMelody, matchedMelody))
            {
                matchedFloorIndex = i;
                break;
            }
        }

        if (matchedFloorIndex == currentFloorIndex)
        {
            Debug.Log($"[ELEVATOR] Melody matches current floor ({matchedFloorIndex}). No movement needed.");
            PlaySuccessFeedback();
            return;
        }

        if (matchedFloorIndex != -1)
        {
            PlaySuccessFeedback();
            StartMovementToFloor(matchedFloorIndex);
        }
    }

    private void HandleMelodyFailed(Transform author)
    {
        Debug.Log("<color=red>[ELEVATOR] Sequence mismatch. Clearing buffer.</color>");
        PlayFailFeedback();
    }

    // ==========================================
    // MOVEMENT & PHYSICS
    // ==========================================

    private void StartMovementToFloor(int targetFloorIndex)
    {
        isMoving = true;
        notesListener.wantsToListen = false;

        int distanceInFloors = Mathf.Abs(targetFloorIndex - currentFloorIndex);
        int totalSubBeats = distanceInFloors * subBeatsPerFloor;

        startPosition = rb.position;
        targetPosition = GetWorldPositionForFloor(targetFloorIndex);

        float secondsPerSubBeat = GetSecondsPerSubBeat();
        moveDuration = totalSubBeats * secondsPerSubBeat;
        moveProgress = 0f;

        Debug.Log($"<color=yellow>[ELEVATOR] Moving from Floor {currentFloorIndex} to Floor {targetFloorIndex}. Duration: {moveDuration:F2}s ({totalSubBeats} subbeats).</color>");

        if (currentWaitHandle != null && !currentWaitHandle.IsCompleted)
        {
            currentWaitHandle.Cancel();
        }

        currentWaitHandle = RhythmBeatWaiter.WaitForSubBeats(totalSubBeats, BeatWaitMode.Immediate, () =>
        {
            OnArrivedAtFloor(targetFloorIndex);
        });
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        moveProgress += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(moveProgress / moveDuration);

        rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, t));
    }

    private void OnArrivedAtFloor(int arrivedFloorIndex)
    {
        rb.MovePosition(targetPosition);
        currentFloorIndex = arrivedFloorIndex;
        isMoving = false;

        Debug.Log($"<color=green>[ELEVATOR] Arrived at Floor {currentFloorIndex}.</color>");

        if (isPlayerOnBoard)
        {
            notesListener.wantsToListen = true;
        }
    }

    private Vector3 GetWorldPositionForFloor(int floorIndex)
    {
        if (floors == null || floorIndex < 0 || floorIndex >= floors.Count)
            return transform.position;

        return initialWorldPosition + new Vector3(0f, floors[floorIndex].height, 0f);
    }

    private float GetSecondsPerSubBeat()
    {
        RhythmManager rhythm = FindObjectOfType<RhythmManager>();
        if (rhythm != null && rhythm.bpm > 0)
        {
            return (60f / rhythm.bpm) / 4f;
        }
        return 0.25f;
    }

    private void PlayNoteFeedback(notesEnum note) { }
    private void PlaySuccessFeedback() { }
    private void PlayFailFeedback() { }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 origin = Application.isPlaying ? initialWorldPosition : transform.position;

        if (floors == null) return;

        for (int i = 0; i < floors.Count; i++)
        {
            Vector3 floorWorldPos = origin + new Vector3(0f, floors[i].height, 0f);

            Gizmos.color = (i == currentFloorIndex) ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(floorWorldPos, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(floorWorldPos, floorWorldPos + transform.right * 0.8f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(floorWorldPos, floorWorldPos + transform.up * 0.8f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(floorWorldPos, floorWorldPos + transform.forward * 0.8f);

            UnityEditor.Handles.Label(floorWorldPos + Vector3.up * 0.4f, $"Floor {i}");
        }
    }
#endif
}