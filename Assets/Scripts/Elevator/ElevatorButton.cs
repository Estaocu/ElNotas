using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorButton : MonoBehaviour
{

    [SerializeField] private int floor;
    private Elevator elevator;
    void Start()
    {
        elevator = transform.parent.GetComponentInChildren<Elevator>();
        if (elevator == null) Debug.LogError("No Elevator detected in siblings");
    }

    public void CallElevator()
    {
        elevator.StartMovementToFloor(floor);
        Debug.Log($"Elevator called, coming to floor {floor}");
    }
}
