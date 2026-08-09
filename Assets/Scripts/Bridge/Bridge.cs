using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CMF;
using Unity.VisualScripting;

public class Bridge : MonoBehaviour
{
    [Header("Bridge Sequence")]
    [SerializeField] private Transform spawnPoint;
    public BridgeSpawn currentSpawner;
    [SerializeField] private BridgeTile previousTile;
    [SerializeField] private BridgeTile currentTile;
    [SerializeField] private BridgeTile nextTile;
    public List<BridgeSpawn> spawners = new List<BridgeSpawn>();

    [Header("Note Actions")]
    [SerializeField] private InputActionReference note1Action;
    [SerializeField] private InputActionReference note2Action;
    [SerializeField] private InputActionReference note3Action;
    [SerializeField] private InputActionReference note4Action;

    [Header("Player References")]
    [SerializeField] private AdvancedWalkerController walker;
    [SerializeField] private Mover mover;
    [SerializeField] private ActionMapsManager actionMap;
    [SerializeField] private AbyssRaycast playerJump;
    

    [Header("Trajectory Settings")]
    [SerializeField] private float apexHeight = 3f;
    [SerializeField] private int gizmoResolution = 30;
    [SerializeField] private Color trajectoryColor = Color.green;
    [SerializeField] private Color gridGizmoColor = new Color(0f, 1f, 1f, 0.35f);

    

    private Dictionary<Vector2Int, BridgeTile> tileGrid = new Dictionary<Vector2Int, BridgeTile>();

    private void Awake()
    {
        InitializeGrid();
        spawners = new List<BridgeSpawn>(GetComponentsInChildren<BridgeSpawn>(true));
   }
   

    private void OnTransformChildrenChanged()
    {
        InitializeGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitializeGrid();
        InitializeSpawners();
    }
#endif

    public void InitializeGrid()
    {
        if (tileGrid == null)
        {
            tileGrid = new Dictionary<Vector2Int, BridgeTile>();
        }

        tileGrid.Clear();
        BridgeTile[] allTiles = GetComponentsInChildren<BridgeTile>(true);

        foreach (BridgeTile tile in allTiles)
        {
            if (tile == null) continue;

            Vector2Int pos = new Vector2Int(tile.xCoord, tile.yCoord);
            if (!tileGrid.ContainsKey(pos))
            {
                tileGrid.Add(pos, tile);
            }
            else
            {
                Debug.LogWarning($"Bridge: Duplicate coordinates ({tile.xCoord}, {tile.yCoord}) detected on '{tile.gameObject.name}'. Skipping assignment.", tile);
            }
        }
    }

    public void InitializeSpawners()
    {
        spawners = new List<BridgeSpawn>(GetComponentsInChildren<BridgeSpawn>(true));
    }

    public void InitializeBridgeFromSpawn(BridgeTile firstTile, Transform spawnJumpTarget)
    {
        //playerInput.SwitchCurrentActionMap("Notes");
        actionMap.SetNotesInput();
        playerJump.PreventJump();

        spawnPoint = spawnJumpTarget;
        currentTile = null;
        previousTile = null;
        nextTile = firstTile;

        notesEnum initialNote = (notesEnum)Random.Range(0, 4);
        InputActionReference actionRef = GetActionForNote(initialNote);

        firstTile.Appear();
        firstTile.SetNoteAndGlyph(initialNote, actionRef);
    }

    public void ProcessSpawnNoteHit(notesEnum playedNote)
    {
        Debug.Log("Received Note!");
        if (currentTile == null && nextTile != null)
        {
            if (nextTile.note == playedNote)
            {
                JumpToNextTile(spawnPoint);
                Debug.Log("Jumped");
            }
        }
        Debug.Log("Something went wrong");
    }

    public void ProcessTileNoteHit(BridgeTile hitTile, notesEnum playedNote)
    {
        if (hitTile == null || !hitTile.gameObject.activeSelf) return;

        if (currentTile == null && nextTile != null)
        {
            if (hitTile == nextTile && hitTile.note == playedNote)
            {
                JumpToNextTile(spawnPoint);
            }
            return;
        }

        if (currentTile == null) return;

        Vector2Int currentPos = new Vector2Int(currentTile.xCoord, currentTile.yCoord);
        Vector2Int hitPos = new Vector2Int(hitTile.xCoord, hitTile.yCoord);

        int distance = Mathf.Abs(hitPos.x - currentPos.x) + Mathf.Abs(hitPos.y - currentPos.y);

        if (distance == 1 && hitTile != previousTile && hitTile.note == playedNote)
        {
            nextTile = hitTile;
            JumpToNextTile();
        }
    }

    public void OnTileReached(BridgeTile reachedTile)
    {

        previousTile = currentTile;
        currentTile = reachedTile;
        spawnPoint = currentTile.jumpTarget;

        AssignNotesToTiles();
    }

    public void JumpToNextTile(Transform customStartPoint = null)
    {
        if (nextTile == null)
        {
            Debug.LogWarning("Bridge: Missing nextTile reference to perform the jump.");
            return;
        }

        JumpToTarget(nextTile.JumpTarget, customStartPoint);
    }

    /// <summary>
    /// Realiza el salto parabólico directamente hacia el punto final fuera del puente.
    /// </summary>
    /// <summary>
/// Realiza el salto parabólico directamente hacia el punto final fuera del puente.
/// </summary>
public void JumpToEnd(Transform endPoint)
{
    if (endPoint == null)
    {
        Debug.LogWarning("Bridge: Missing endPoint target to jump to end.");
        return;
    }

    // El origen del salto es la casilla actual (la última tile que el jugador acaba de pisar)
    Transform startPoint = currentTile != null ? currentTile.JumpTarget : null;

    // Ejecuta el salto desde la última casilla hacia el punto final de salida
    JumpToTarget(endPoint, startPoint);
}

/// <summary>
/// Método central que ejecuta el cálculo de trayectoria y físicas de CMF desde un origen A hacia un destino B.
/// </summary>
public void JumpToTarget(Transform targetPoint, Transform customStartPoint = null)
{
    if (targetPoint == null || walker == null || mover == null)
    {
        Debug.LogWarning("Bridge: Missing references or target point to perform the jump.");
        return;
    }

    // Si se le pasa un customStartPoint explícito tiene prioridad; si no, utiliza la casilla actual
    Transform pointA = customStartPoint != null ? customStartPoint : (currentTile != null ? currentTile.JumpTarget : null);
    Transform pointB = targetPoint;

    if (pointA == null || pointB == null)
    {
        Debug.LogWarning("Bridge: Missing jump origin (Point A) or target (Point B).");
        return;
    }

    // 1. Sincroniza la posición inicial con el motor de físicas de Unity
    walker.transform.position = pointA.position;
    Physics.SyncTransforms();
    mover.CheckForGround();

    // 2. Calcula la velocidad vectorial necesaria para la parábola
    Vector3 launchVelocity = CalculateLaunchVelocity(pointA.position, pointB.position, walker.gravity);

    // 3. Resetea e inyecta la inercia en el AdvancedWalkerController
    walker.SetMomentum(Vector3.zero);
    walker.SetMomentum(launchVelocity);
}

    public void AssignNotesToTiles()
    {
        if (currentTile == null) return;

        List<BridgeTile> adjacentTiles = new List<BridgeTile>();

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        Vector2Int currentPos = new Vector2Int(currentTile.xCoord, currentTile.yCoord);

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkPos = currentPos + dir;

            if (tileGrid.TryGetValue(checkPos, out BridgeTile adjTile))
            {
                if (adjTile != previousTile)
                {
                    adjacentTiles.Add(adjTile);
                }
            }
        }

        List<notesEnum> availableNotes = new List<notesEnum>
        {
            (notesEnum)0,
            (notesEnum)1,
            (notesEnum)2,
            (notesEnum)3
        };

        foreach (BridgeTile tile in adjacentTiles)
        {
            if (availableNotes.Count == 0) break;

            int randomIndex = Random.Range(0, availableNotes.Count);
            notesEnum selectedNote = availableNotes[randomIndex];

            InputActionReference actionRef = GetActionForNote(selectedNote);

            tile.Appear();
            tile.SetNoteAndGlyph(selectedNote, actionRef);

            availableNotes.RemoveAt(randomIndex);
        }
    }

    private InputActionReference GetActionForNote(notesEnum note)
    {
        int noteIndex = (int)note;
        switch (noteIndex)
        {
            case 0: return note1Action;
            case 1: return note2Action;
            case 2: return note3Action;
            case 3: return note4Action;
            default: return null;
        }
    }

    public void SetNextTile(BridgeTile newNextTile)
    {
        previousTile = currentTile;
        currentTile = nextTile;
        nextTile = newNextTile;
    }

    private Vector3 CalculateLaunchVelocity(Vector3 startPos, Vector3 targetPos, float gravity)
    {
        float displacementY = targetPos.y - startPos.y;
        float adjustedApex = Mathf.Max(apexHeight, displacementY + 1f);

        Vector3 displacementXZ = new Vector3(targetPos.x - startPos.x, 0, targetPos.z - startPos.z);

        float timeUp = Mathf.Sqrt(2f * adjustedApex / gravity);
        float timeDown = Mathf.Sqrt(2f * (adjustedApex - displacementY) / gravity);
        float totalTime = timeUp + timeDown;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(2f * gravity * adjustedApex);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }

    public void TraceParabole()
    {
        if (nextTile == null) return;

        Transform pointA = currentTile != null ? currentTile.JumpTarget : spawnPoint;
        Transform pointB = nextTile.JumpTarget;

        if (pointA == null || pointB == null) return;

        float previewGravity = walker != null ? walker.gravity : 20f;

        Gizmos.color = trajectoryColor;
        DrawParabolaArc(pointA.position, pointB.position, previewGravity);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pointA.position, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pointB.position, 0.2f);
    }

    private void DrawAllConnectedParabolas()
    {
        if (tileGrid == null || tileGrid.Count == 0)
        {
            InitializeGrid();
        }

        Vector2Int[] checkDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1)
        };

        float previewGravity = walker != null ? walker.gravity : 20f;
        Gizmos.color = gridGizmoColor;

        foreach (var kvp in tileGrid)
        {
            Vector2Int currentPos = kvp.Key;
            BridgeTile currentTileRef = kvp.Value;

            if (currentTileRef == null || currentTileRef.JumpTarget == null) continue;

            foreach (Vector2Int dir in checkDirections)
            {
                Vector2Int neighborPos = currentPos + dir;
                if (tileGrid.TryGetValue(neighborPos, out BridgeTile neighborTile))
                {
                    if (neighborTile == null || neighborTile.JumpTarget == null) continue;

                    DrawParabolaArc(currentTileRef.JumpTarget.position, neighborTile.JumpTarget.position, previewGravity);
                }
            }
        }
    }

    private void DrawParabolaArc(Vector3 startPos, Vector3 targetPos, float gravity)
    {
        float displacementY = targetPos.y - startPos.y;
        float adjustedApex = Mathf.Max(apexHeight, displacementY + 1f);

        Vector3 displacementXZ = new Vector3(targetPos.x - startPos.x, 0, targetPos.z - startPos.z);

        float timeUp = Mathf.Sqrt(2f * adjustedApex / gravity);
        float timeDown = Mathf.Sqrt(2f * (adjustedApex - displacementY) / gravity);
        float totalTime = timeUp + timeDown;

        if (totalTime <= 0f) return;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(2f * gravity * adjustedApex);
        Vector3 velocityXZ = displacementXZ / totalTime;
        Vector3 launchVelocity = velocityXZ + velocityY;

        Vector3 previousPoint = startPos;

        for (int i = 1; i <= gizmoResolution; i++)
        {
            float simulationTime = (i / (float)gizmoResolution) * totalTime;
            Vector3 currentPoint = startPos + (launchVelocity * simulationTime) + (Vector3.down * (0.5f * gravity * simulationTime * simulationTime));

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }

    private void OnDrawGizmos()
    {
        DrawAllConnectedParabolas();
        TraceParabole();
    }

    public bool CompareNotes(notesEnum incomingNote)
    {
        // CASO 1: Aún no estamos en el puente
        if (currentTile == null)
        {
            if (nextTile != null && nextTile.note == incomingNote)
            {
                JumpToNextTile(spawnPoint);
                return true;
            }
            return false;
        }

        // CASO 2: Ya estamos posicionados sobre una casilla del puente
        Vector2Int currentPos = new Vector2Int(currentTile.xCoord, currentTile.yCoord);

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkPos = currentPos + dir;

            if (tileGrid.TryGetValue(checkPos, out BridgeTile adjTile))
            {
                if (adjTile == null || !adjTile.gameObject.activeSelf) continue;

                if (adjTile != previousTile && adjTile.note == incomingNote)
                {
                    nextTile = adjTile;
                    JumpToNextTile();
                    return true;
                }
            }
        }

        return false;
    }

    public void EndBridge()
    {
        actionMap.SetPlayerInput();
        playerJump.EnableJump();
        currentSpawner = null;

        foreach(BridgeSpawn spawner in spawners)
        {
            spawner.EndBridge();
        }

        // BridgeTile[] allTiles = GetComponentsInChildren<BridgeTile>(true);

        // foreach (BridgeTile tile in allTiles)
        // {
        //     tile.Disappear();
        // }

    }
}