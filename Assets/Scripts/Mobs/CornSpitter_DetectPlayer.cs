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
        GameObject myCorn = PoolManager.Instance.GetObject(cornPrefab);
        if (myCorn == null)
        {
            cornSpawned = false;
            return;
        }
        myCorn.transform.position = cornSpawnpoint.position;
        myCorn.transform.rotation = Quaternion.identity;

        HomingProjectile script = myCorn.GetComponent<HomingProjectile>();
        if (script != null)
        {
            script.player = other.gameObject;
            beatSinger.Sing();
        }
    }
}
