using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Checkpoint : MonoBehaviour
{
    private Transform coord;
    public Transform Coord => coord;
    private CheckpointZone zone;
    public CheckpointZone Zone => zone;

    private CheckpointManager manager;

    public bool isFirstCheckpoint = false;


    void Awake()
    {
        zone = GetComponentInParent<CheckpointZone>();
        coord = GetComponentInChildren<Transform>();
        
        if (coord == null)
        {
            Transform foundCoord = transform.Find("Coord");
            coord = foundCoord != null ? foundCoord : transform;
        }
    }

    void Start()
    {
        manager = FindFirstObjectByType<CheckpointManager>();
    }

    public void OnPlayerEnter()
    {
        zone.SetLatestPoint(this);
        Debug.Log("Player entered Checkpoint");
        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
        manager.TEXTDEBUG.SetText
        ($"Zone: {zone.zoneID}\n Rhythm: {zone.hasRhythm}");
        return;
    }
}
