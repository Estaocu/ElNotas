using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SingleNotesListener : MonoBehaviour, IPoolable
{
    public enum ListenerMode
    {
        PredefinedMelodies,
        RecordAndBounce
    }

    [Header("General Settings")]
    public ListenerMode currentMode = ListenerMode.PredefinedMelodies;
    public bool wantsToListen = true;

    [Header("Universal Settings (Predefined Melodies)")]
    [Tooltip("Length of the sequence to listen to before validating.")]
    public int maxBufferLength = 4;
    [Tooltip("Clear buffer if combination fails?")]
    public bool clearOnFail = true;

    private List<notesEnum[]> targetMelodies = new List<notesEnum[]>();
    private List<notesEnum> currentBuffer = new List<notesEnum>();

    public UnityEvent<notesEnum, Transform> OnNoteReceivedEvent;
    public UnityEvent<notesEnum[], Transform> OnMelodyMatchedEvent;
    public UnityEvent<Transform> OnMelodyFailedEvent;

    [Header("Legacy Settings (Record & Bounce)")]
    [SerializeField] private notesEnum[] currentMelody = new notesEnum[2];
    [SerializeField] private notesEnum[] externalMelody = new notesEnum[2];
    public notesEnum[] desiredMelody = new notesEnum[2];
    private int storedNotes = 0;
    private int externalNotesStored = 0;
    private IControllableProjectile homingProjectile;

    private void Awake()
    {
        if (currentMode == ListenerMode.RecordAndBounce && transform.parent != null)
        {
            homingProjectile = transform.parent.GetComponent<IControllableProjectile>();
        }
    }

    public void OnSpawn()
    {
        wantsToListen = true;

        if (currentMode == ListenerMode.RecordAndBounce)
        {
            ClearMelody(currentMelody);
            ClearMelody(externalMelody);
            ClearMelody(desiredMelody);
            storedNotes = 0;
            externalNotesStored = 0;
        }
        else
        {
            currentBuffer.Clear();
        }
    }

    public void SetDesiredMelodies(List<notesEnum[]> newMelodies)
    {
        targetMelodies = new List<notesEnum[]>(newMelodies);
    }

    public void ClearDesiredMelodies()
    {
        targetMelodies.Clear();
    }

    public void ResetBuffer()
    {
        currentBuffer.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentMode == ListenerMode.RecordAndBounce)
        {
            ProcessIncomingObject(other.gameObject);
        }
    }

    public void ProcessIncomingObject(GameObject targetObject)
    {
        if (!wantsToListen || targetObject == null) return;

        if (!targetObject.TryGetComponent<SingleNoteSoundwave>(out SingleNoteSoundwave sw))
        {
            sw = targetObject.GetComponentInParent<SingleNoteSoundwave>();
            if (sw == null) sw = targetObject.GetComponentInChildren<SingleNoteSoundwave>();
        }

        if (sw == null) return;

        if (currentMode == ListenerMode.RecordAndBounce)
        {
            HandleRecordAndBounceLogic(sw);
        }
        else if (currentMode == ListenerMode.PredefinedMelodies)
        {
            HandlePredefinedMelodiesLogic(sw);
        }
    }

    private void HandlePredefinedMelodiesLogic(SingleNoteSoundwave sw)
    {
        Transform authorTransform = sw.author != null ? sw.author.transform : transform;
        
        OnNoteReceivedEvent?.Invoke(sw.myNote, authorTransform);
        currentBuffer.Add(sw.myNote);

        if (currentBuffer.Count >= maxBufferLength)
        {
            CheckBufferAgainstTargetMelodies(authorTransform);
        }
    }

    private void CheckBufferAgainstTargetMelodies(Transform author)
    {
        bool matchFound = false;
        notesEnum[] matchedMelody = null;

        foreach (notesEnum[] melody in targetMelodies)
        {
            if (melody.Length == maxBufferLength && CompareArrays(currentBuffer.ToArray(), melody))
            {
                matchFound = true;
                matchedMelody = melody;
                break;
            }
        }

        if (matchFound)
        {
            OnMelodyMatchedEvent?.Invoke(matchedMelody, author);
            currentBuffer.Clear();
        }
        else
        {
            OnMelodyFailedEvent?.Invoke(author);
            if (clearOnFail)
            {
                currentBuffer.Clear();
            }
        }
    }

    public static bool CompareArrays(notesEnum[] a, notesEnum[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private void HandleRecordAndBounceLogic(SingleNoteSoundwave sw)
    {
        if (homingProjectile != null && sw.author != null) 
            homingProjectile.SaveNotePosition(sw.author.transform);

        switch (storedNotes)
        {
            case 0:
                AddCurrentNote(sw.myNote);
                break;
            case 1:
                CompareWithFirstNote(sw.myNote, sw.author);
                break;
            case 2: 
                AddExternalNote(sw.myNote);
                CompareMelodies(sw.author);
                break;
        }
    }

    private void AddCurrentNote(notesEnum note)
    {
        if (storedNotes >= currentMelody.Length) return;
        currentMelody[storedNotes] = note;
        storedNotes++;
    }

    private void AddExternalNote(notesEnum note)
    {
        if (externalNotesStored >= externalMelody.Length) return;
        externalMelody[externalNotesStored] = note;
        externalNotesStored++;
    }

    private void CompareWithFirstNote(notesEnum newNote, GameObject author)
    {
        if (newNote == currentMelody[0])
        {
            ClearMelody(currentMelody);
            storedNotes = 0;
            Debug.Log("Note repeated. Corn deflated.");
            return;
        }

        currentMelody[1] = newNote;
        desiredMelody[0] = currentMelody[0];
        desiredMelody[1] = currentMelody[1];

        ClearMelody(currentMelody);
        storedNotes = 2;

        if (homingProjectile != null && author != null) homingProjectile.Launch(author);
    }

    private void CompareMelodies(GameObject author)
    {
        int lastNoteIndex = externalNotesStored - 1;
        if (lastNoteIndex < 0) return;
        notesEnum noteToVerify = externalMelody[lastNoteIndex];

        bool isCorrect = false;
        if (lastNoteIndex == 0 && noteToVerify == desiredMelody[1]) isCorrect = true;
        else if (lastNoteIndex == 1 && noteToVerify == desiredMelody[0]) isCorrect = true;

        if (isCorrect)
        {
            if (lastNoteIndex == 1)
            {
                if (homingProjectile != null && author != null) homingProjectile.Launch(author);
                RearrangeMelody(desiredMelody);
                ClearExternalMelody();
            }
        }
        else
        {
            ClearExternalMelody();
        }
    }

    private void ClearExternalMelody()
    {
        ClearMelody(externalMelody);
        externalNotesStored = 0;
    }

    private void RearrangeMelody(notesEnum[] melody)
    {
        (desiredMelody[0], desiredMelody[1]) = (desiredMelody[1], desiredMelody[0]);
    }

    private void ClearMelody(notesEnum[] whichMelody)
    {
        for (int i = 0; i < whichMelody.Length; i++) whichMelody[i] = default;
    }
}