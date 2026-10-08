using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorButton : MonoBehaviour
{

    [SerializeField] private int floor;
    private Elevator elevator;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        elevator = transform.parent.GetComponentInChildren<Elevator>();
    }

    public void CallElevator()
    {
        elevator.StartMovementToFloor(floor);
        audioSource.PlayOneShot(audioSource.clip);
        //Debug.Log($"Elevator called, coming to floor {floor}");
    }
}
