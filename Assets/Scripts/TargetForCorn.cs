using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetForCorn : MonoBehaviour
{
    [SerializeField] private Transform aimTransform;

    public Transform AimTransform => aimTransform != null ? aimTransform : transform;

    private void OnDrawGizmosSelected()
    {
        // Visual aid in Editor to see where projectiles will aim
        if (aimTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aimTransform.position, 0.2f);
        }
    }
}