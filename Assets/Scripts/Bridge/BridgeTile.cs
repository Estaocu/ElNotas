using UnityEngine;
using UnityEngine.InputSystem;
using ElNotas.Input.Glyphs;

[RequireComponent(typeof(Collider))]
public class BridgeTile : MonoBehaviour
{
    public int xCoord;
    public int yCoord;
    
    public notesEnum note; 
    public bool beingStepped = false;

    [SerializeField] private Transform jumpTarget;
    [SerializeField] private SingleNotesListener listener;
    [SerializeField] private Bridge bridge;
    [SerializeField] private BindingGlyphView glyphView;

    private InputActionReference currentActionRef;

    public Transform JumpTarget => jumpTarget;

    private void Awake()
    {
        if (glyphView == null)
        {
            glyphView = GetComponentInChildren<BindingGlyphView>(true);
        }
    }

    private void OnEnable()
    {
        if (listener != null)
        {
            listener.OnNoteReceivedEvent.AddListener(OnNoteReceived);
        }

        UpdateGlyphDisplay();
    }

    private void OnDisable()
    {
        if (listener != null)
        {
            listener.OnNoteReceivedEvent.RemoveListener(OnNoteReceived);
        }
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        UpdateGlyphDisplay();
    }

    public void SetAsCurrentTile()
    {
        beingStepped = true;
    }

    public void OnEntityExited()
    {
        beingStepped = false;
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

    private void OnNoteReceived(notesEnum receivedNote, Transform author)
    {
        if (bridge != null)
        {
            bridge.ProcessTileNoteHit(this, receivedNote);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !beingStepped)
        {
            if (bridge != null)
            {
                bridge.OnTileReached(this);
            }
        }
    }
}