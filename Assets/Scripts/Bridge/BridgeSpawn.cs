using UnityEngine;

public class BridgeSpawn : MonoBehaviour, IReactToMelody, IBridgeListen
{
    [SerializeField] private bool isMother;
    [SerializeField] private bool unlocked;
    [SerializeField] private SingleNotesListener listener;
    [SerializeField] private BridgeTile closestTile;
    [SerializeField] private Transform jumpTarget;
    [SerializeField] private Bridge bridge;
    private bool bridgeStarted = false;

    public void React(Melody receivedMelody)
    {
        if (!unlocked) return;
        if (bridgeStarted) return;

        Debug.Log($"Melody {receivedMelody} received");
        bridge.InitializeBridgeFromSpawn(closestTile, jumpTarget);
        Debug.Log($"Initial tile [{closestTile.xCoord}, {closestTile.yCoord}] activated.");

        bridgeStarted = true;
    }

    public void OnNoteReceived(GameObject detectedObj)
    {
        if (!bridgeStarted || !unlocked) return;
        if (detectedObj.TryGetComponent<SingleNoteSoundwave>(out var noteWave))
        {
            // Ejecutamos la lógica original con la nota obtenida
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
        bridge.JumpToNextTile();
    }
}