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
        // if (other.CompareTag("Soundwave")) return;

        // Usar transform.root para cubrir el caso en que el sender tenga colliders en hijos
        if (projectile.Sender != null && other.transform.root.gameObject == projectile.Sender)
        {
            Debug.Log("Me he chocado con quien me envia");
            return;
        }

        projectile.SetTarget(other.gameObject);
        projectile.SetMovementMode(Mode.Homing, projectile.Sender);
    }
}
