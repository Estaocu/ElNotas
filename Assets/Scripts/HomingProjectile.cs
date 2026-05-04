using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private float speed = 5f;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        
    }

    public void MoveForward(Transform noteTransform)
    {
        moveDirection = (noteTransform.position - gameObject.transform.position).normalized;
        rb.velocity = moveDirection * speed;
    }

    public void MoveTowardsTarget()
    {
        
    }
}
