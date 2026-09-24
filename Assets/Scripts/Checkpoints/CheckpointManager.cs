using System.Collections;
using System.Collections.Generic;
using CMF;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public CheckpointZone currentZone;
    private CheckpointZone[] zones;

    [SerializeField] private Transform playerOverride;

    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private Mover _playerMover;
    private AdvancedWalkerController _playerWalker;


#if UNITY_EDITOR
    private void OnValidate()
    {
        CountZones();
    }
#endif

    void Start()
    {
        ResolvePlayer();
        CountZones();
        currentZone = zones[0];
    }

    private void CountZones()
    {
        zones = GetComponentsInChildren<CheckpointZone>(true);
        currentZone = zones[0];
    }
    

    public void SendPlayerToPoint(bool dead)
{
    if (currentZone == null)
    {
        Debug.LogError("[CHECKPOINTS] currentZone no está asignada.");
        return;
    }

    Checkpoint targetCheckpoint = dead 
        ? (currentZone.points.Length > 0 ? currentZone.points[0] : null) 
        : currentZone.latestPoint;

    if (targetCheckpoint == null || targetCheckpoint.Coord == null)
    {
        Debug.LogError("[CHECKPOINTS] No se pudo encontrar un punto de destino o Coord válido.");
        return;
    }

    Transform target = targetCheckpoint.Coord;

    // 1. Resetear el movimiento y la inercia en el sistema CMF
    if (_playerMover != null)
    {
        _playerMover.SetVelocity(Vector3.zero);
    }

    // 2. Resetear del todo la física del Rigidbody
    if (_playerRigidbody != null)
    {
        _playerRigidbody.velocity = Vector3.zero;
        _playerRigidbody.angularVelocity = Vector3.zero;
        
        // Es vital mover el Rigidbody directamente si CMF trabaja sobre físicas
        _playerRigidbody.position = target.position;
        _playerRigidbody.rotation = target.rotation;
    }

    // 3. Mover la transformada raíz
    if (playerOverride != null)
    {
        playerOverride.position = target.position;
        playerOverride.rotation = target.rotation;
    }
}

    private void ResolvePlayer()
    {
        Transform source = playerOverride != null
            ? playerOverride
            : GameObject.Find("ThirdPersonWalker_B")?.transform;

        if (source == null)
        {
            Debug.LogError("[CHECKPOINTS] Player no encontrado. Asigna 'Player Override' o comprueba que 'ThirdPersonWalker_B' está en escena.");
            return;
        }

        playerOverride = source;
        _playerRigidbody = source.GetComponentInChildren<Rigidbody>();
        _playerMover = source.GetComponentInChildren<Mover>();
        
        Debug.Log($"[CHECKPOINTS] Player resuelto: {source.name}");
    }
}
