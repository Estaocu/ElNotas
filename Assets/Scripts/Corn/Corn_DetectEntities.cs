using UnityEngine;

public class Corn_DetectEntities : MonoBehaviour
{
    private IControllableProjectile projectile;

    void Awake()
    {
        projectile = transform.parent.GetComponent<IControllableProjectile>();
    }

    void OnTriggerStay(Collider other)
    {
        if (projectile.Sender != null && other.transform.root.gameObject == projectile.Sender) return;

        projectile.SetTarget(other.gameObject);
        
        projectile.SetMovementMode(Mode.Homing, projectile.Sender);
    }
}
