using UnityEngine;

public class BridgeSpawn : MonoBehaviour, IReactToMelody, IBridgeListen
{
    [SerializeField] private bool isMother;
    [SerializeField] private bool unlocked;
    [SerializeField] private SingleNotesListener listener;
    [SerializeField] private BridgeTile closestTile;
    public Transform jumpTarget;
    [SerializeField] private Bridge bridge;
    public bool currentSpawner = false;

    
    private bool bridgeStarted = false;

    public void React(Melody receivedMelody)
    {
        if (!unlocked || bridgeStarted) return;

        Debug.Log($"Melody {receivedMelody} received");
        bridge.InitializeBridgeFromSpawn(closestTile, jumpTarget);
        Debug.Log($"Initial tile [{closestTile.xCoord}, {closestTile.yCoord}] activated.");

        // Mark as initialized so it can receive notes, but bridgeStarted remains false
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
        // CompareNotes returns true only if a valid tile was found and jump was performed
        bool noteMatched = bridge.CompareNotes(incomingNote);

        if (noteMatched)
        {
            bridgeStarted = true;
        }
    }

    public void Bloom()
    {
        unlocked = true;
        Debug.Log("Bridge Spawner Bloomed!");
    }
}