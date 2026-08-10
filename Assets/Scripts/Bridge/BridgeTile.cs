using UnityEngine;
using UnityEngine.InputSystem;
using ElNotas.Input.Glyphs;
using NaughtyAttributes;
using Unity.VisualScripting;

[RequireComponent(typeof(Collider))]
public class BridgeTile : MonoBehaviour
{
    public int xCoord;
    public int yCoord;
    
    public notesEnum note; 
    public bool beingStepped = false;

    public Transform jumpTarget;
    [SerializeField] private Bridge bridge;
    [SerializeField] private BindingGlyphView glyphView;

    public bool isEnd = false;
    [ShowIf("isEnd")]
    [SerializeField] private BridgeSpawn assignedSpawner;

    private InputActionReference currentActionRef;

    public Transform JumpTarget => jumpTarget;

    private BeatWaitHandle currentWaitHandle;

    private void Awake()
    {
        if (glyphView == null)
        {
            glyphView = GetComponentInChildren<BindingGlyphView>(true);
        }
    }

    private void OnEnable()
    {
        UpdateGlyphDisplay();
    }

    private void OnDisable()
    {

    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        UpdateGlyphDisplay();

        currentWaitHandle = RhythmBeatWaiter.WaitForSubBeats(16, BeatWaitMode.Immediate, Disappear);
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }

    public void SetAsCurrentTile()
{
    beingStepped = true;

    bridge.OnTileReached(this);

    currentWaitHandle = RhythmBeatWaiter.WaitForSubBeats(8, BeatWaitMode.Immediate, Disappear);

    if (isEnd)
    {
        if (assignedSpawner != bridge.currentSpawner)
        {
            bridge.JumpToEnd(assignedSpawner.jumpTarget);
            bridge.EndBridge();
            assignedSpawner.Bloom();
        }
    }
}

    public void OnEntityExited()
    {
        beingStepped = false;
        note = default;
        UpdateGlyphDisplay();
    }

    public void SetNoteAndGlyph(notesEnum newNote, InputActionReference actionRef)
    {
        note = newNote;
        currentActionRef = actionRef;

        UpdateGlyphDisplay();
    }

    public void UpdateGlyphDisplay()
    {
        if (glyphView == null)
        {
            glyphView = GetComponentInChildren<BindingGlyphView>(true);
        }

        if (glyphView != null && currentActionRef != null)
        {
            glyphView.action = currentActionRef;
            glyphView.Refresh();
        }
    }

    public void OnNoteReceived(GameObject detectedObj)
    {
        if (bridge != null)
        {
            if (!beingStepped) return;

            if (detectedObj.TryGetComponent<SingleNoteSoundwave>(out var noteWave))
        {
            ProcessNote(noteWave.myNote);
            Debug.Log($"Nota {noteWave.myNote}");
        }
        else
        {
            Debug.LogWarning($"[BridgeSpawn] El objeto '{detectedObj.name}' no tiene el componente SingleNoteSoundwave.", this);
        }

        
        }
    }

    public void ProcessNote(notesEnum incomingNote)
    {
        bridge.CompareNotes(incomingNote);
    }
}