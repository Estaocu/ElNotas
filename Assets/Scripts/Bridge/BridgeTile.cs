using UnityEngine;
using UnityEngine.InputSystem;
using ElNotas.Input.Glyphs;
using NaughtyAttributes;
using Unity.VisualScripting;
using System.Collections;

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

    private RhythmClock rhythmClock;

    private Coroutine dieRoutine;

    [SerializeField] int dissapearSubbeats = 8;




    private void Awake()
    {
        rhythmClock = FindFirstObjectByType<RhythmClock>();

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

        dieRoutine = StartCoroutine(DissapearRoutine(12));
    }

    public void Disappear()
    {
        gameObject.SetActive(false);
    }

    public void SetAsCurrentTile()
{
    beingStepped = true;

    bridge.OnTileReached(this);

    dieRoutine = StartCoroutine(DissapearRoutine(8));

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

    private IEnumerator DissapearRoutine(int subbeats)
    {
        yield return rhythmClock.WaitForSubBeats(subbeats);

        dieRoutine = null;

        Disappear();
    }
}