using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornSpitter_DetectPlayer : MonoBehaviour
{
    private IControllableProjectile projectile;
    [SerializeField] private AddNotesOnBeat beatSinger;
    [SerializeField] private GameObject cornPrefab;
    [SerializeField] private Transform cornSpawnpoint;
    private bool cornSpawned = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Player" || cornSpawned) return;
        cornSpawned = true;
        GameObject instance = Object.Instantiate(cornPrefab, cornSpawnpoint.position, Quaternion.identity);
        HomingProjectile script = instance.GetComponent<HomingProjectile>();

        if (script != null)
        {
            script.player = other.gameObject;
            beatSinger.Sing();
        }
    }
}
