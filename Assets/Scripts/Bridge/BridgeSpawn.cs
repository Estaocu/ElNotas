using UnityEngine;
using CarterGames.Assets.SaveManager;
using Save;
using System.Collections;
using CMF;

public class BridgeSpawn : MonoBehaviour, IReactToMelody, IBridgeListen
{
    [Header("Save System")]
    [Tooltip("ID único para guardar el estado de este spawner. Se genera automáticamente en el Editor.")]
    [SerializeField] private string id;
    private BridgesSaveObject saveObject;

    [Header("Bridge Spawn Settings")]
    [SerializeField] private bool isMother;
    private bool unlocked;
    [SerializeField] private BridgeTile closestTile;
    [SerializeField] private BridgeVine closestVine;
    [HideInInspector] public Transform jumpTarget;
    private Bridge bridge;

    public bool currentSpawner = false;
    private bool bridgeStarted = false;

    void OnEnable()
    {
        if(closestTile != null)
        closestTile.OnFlowerDeath += CancelBridge;
    }

    void OnDisable()
    {
        if(closestTile != null)
        closestTile.OnFlowerDeath -= CancelBridge;
    }

    void Awake()
    {
        bridge = GetComponentInParent<Bridge>();
        jumpTarget = GetComponentInChildren<Transform>();

        if (isMother) unlocked = true;

        if (SaveManager.TryGetGlobalSaveObject(out saveObject))
        {
            Debug.Log("SaveObject global encontrado con éxito.");
            
            // Verificación defensiva por si la lista aún no se ha creado en el ScriptableObject
            if (saveObject.unlockedBridges != null && saveObject.unlockedBridges.Value != null)
            {
                if (saveObject.unlockedBridges.Value.Contains(id))
                {
                    unlocked = true;
                    // Si tu puente necesita inicializarse visualmente al cargar, hazlo aquí
                }
            }
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar el BridgesSaveObject global. Asegúrate de que está creado y registrado en los ajustes del Save Manager.", this);
        }
    }

    public void React(Melody receivedMelody)
    {
        if (!unlocked || bridgeStarted) return;

        Debug.Log($"Melody {receivedMelody} received");
        bridge.InitializeBridgeFromSpawn(closestTile, jumpTarget, closestVine);
        Debug.Log($"Initial tile [{closestTile.xCoord}, {closestTile.yCoord}] activated.");

        bridgeStarted = true;
        currentSpawner = true;
        bridge.currentSpawner = this;
    }

    public void OnNoteReceived(GameObject detectedObj)
    {
        if (!unlocked || !bridgeStarted) return;

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

    public void ProcessNote(notesEnum incomingNote)
    {
        bool noteMatched = bridge.CompareNotes(incomingNote);

        if (noteMatched)
        {
            bridgeStarted = true;
            
            if(closestTile != null)
            closestTile.OnFlowerDeath -= CancelBridge;
        }
    }

    public void Bloom()
    {
        if (unlocked) return;

        unlocked = true;
        Debug.Log("Bridge Spawner Bloomed!");

        if (saveObject != null && saveObject.unlockedBridges != null && saveObject.unlockedBridges.Value != null)
        {
            if (!saveObject.unlockedBridges.Value.Contains(id))
            {
                saveObject.unlockedBridges.Value.Add(id);

                SaveManager.SaveGame();
                Debug.Log("Bridge unlocked and saved");
            }
        }
    }

    public void EndBridge()
    {
        bridgeStarted = false;
        //y luego cosmeticos
    }

    public void CancelBridge()
    {
        EndBridge();
        ActionMapsManager.SetPlayerInput();
    }
}