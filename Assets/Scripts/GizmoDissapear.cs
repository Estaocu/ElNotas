using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoDissapear : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    [Range(0f, 1f)]
    public float alpha = 0.75f;
    private void OnDrawGizmos()
    {
        if (endPoint == null) return;

        // Set the color with custom alpha
        Gizmos.color = new Color(1f, 1f, 0f, alpha); // Yellow with custom alpha

        // Draw the line
        Gizmos.DrawLine(startPoint.position, endPoint.position);


    }
}
