using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnCorn : MonoBehaviour
{
    public Transform respawnLocation;

    void Awake()
    {
        respawnLocation = gameObject.transform;
    }

    void Respawn(Transform transform)
    {
        
    }
}
