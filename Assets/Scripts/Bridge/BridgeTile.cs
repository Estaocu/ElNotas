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

    private int subbeatsAlive = 12;
    
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

    [SerializeField] private float growthTime = 0.2f;

    [SerializeField] private GameObject visuals;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve deathCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Appear()
{
    gameObject.SetActive(true);

    // Stop previous routine if active to avoid stacking
    if (dieRoutine != null)
    {
        StopCoroutine(dieRoutine);
    }

    StartCoroutine(ScaleMeshRoutine(Vector3.zero, Vector3.one, growthTime, growCurve));
    UpdateGlyphDisplay();

    dieRoutine = StartCoroutine(DissapearRoutine(subbeatsAlive));
    
}

    public void Disappear()
    {
        StartCoroutine(ScaleMeshRoutine(Vector3.one, Vector3.zero, growthTime, deathCurve));
    }

    public void SetAsCurrentTile()
    {
        beingStepped = true;

        bridge.OnTileReached(this);

        if (isEnd)
        {
            if (assignedSpawner != bridge.currentSpawner)
            {
                bridge.JumpToEnd(assignedSpawner.jumpTarget);
                assignedSpawner.Bloom();
            }
        }
    }

    private IEnumerator ScaleMeshRoutine(Vector3 initialScale, Vector3 endScale, float growthTime, AnimationCurve curve)
        {
        float elapsedTime = 0f;

            while (elapsedTime < growthTime)
        {
            float progress = elapsedTime / growthTime;

            float curveProgress = curve.Evaluate(progress);

            visuals.transform.localScale = Vector3.Lerp(initialScale, endScale, curveProgress);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        visuals.transform.localScale = endScale;
            
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